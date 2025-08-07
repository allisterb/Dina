# Install latest transformers for Gemma 3N
# !pip install --no-deps transformers>=4.53.1 # Only for Gemma 3N
# !pip install --no-deps --upgrade timm # Only for Gemma 3N

#from huggingface_hub import login
#login()


from enum import Enum
from collections import Counter
import numpy as np
from math import ceil
from tqdm.notebook import tqdm

class Config:
    model_name = "unsloth/gemma-3n-E4B-it"
    dataset_name = "lmassaron/hermes-function-calling-v1"
    output_dir = "gemma-3n-E4B-it-function_calling"
    lora_arguments = {
        "r": 16,
        "lora_alpha": 64,
        "lora_dropout": 0.05,
        "target_modules": [
            "embed_tokens",
            "q_proj",
            "k_proj",
            "v_proj",
            "gate_proj",
            "up_proj",
            "down_proj",
            "o_proj",
            "lm_head",
        ],
    }
    training_arguments = {
        # Basic training configuration
        "num_train_epochs": 1,
        "max_steps": -1,
        "per_device_train_batch_size": 1,
        "per_device_eval_batch_size": 1,
        "gradient_accumulation_steps": 4,
        "max_seq_length": 2048,
        "packing": True,
        # Optimization settings
        "optim": "adamw_torch_fused",
        "learning_rate": 1e-4,
        "weight_decay": 0.1,
        "max_grad_norm": 1.0,
        "lr_scheduler_type": "cosine",
        "warmup_ratio": 0.1,
        # Memory optimization
        "gradient_checkpointing": True,
        "gradient_checkpointing_kwargs": {"use_reentrant": False},
        # Evaluation and saving
        "eval_strategy": "epoch",
        "save_strategy": "epoch",
        "save_total_limit": 2,
        "load_best_model_at_end": True,
        "metric_for_best_model": "eval_loss",
        "greater_is_better": False,
        # Logging and output
        "logging_steps": 5,
        "report_to": "tensorboard",
        "logging_dir": "logs/runs",
        "overwrite_output_dir": True,
        # Model sharing
        "push_to_hub": False,
        "hub_private_repo": False,
    }
    batch_size = 24

class ChatmlSpecialTokens(str, Enum):
    """Enum class defining special tokens used in the ChatML format"""

    tools = "<tools>"
    eotools = "</tools>"
    think = "<think>"
    eothink = "</think>"
    tool_call = "<tool_call>"
    eotool_call = "</tool_call>"
    tool_response = "<tool_response>"
    eotool_response = "</tool_response>"
    pad_token = "<pad>"
    eos_token = "<eos>"

    @classmethod
    def list(cls):
        return [c.value for c in cls]

config = Config()
# compute_dtype = torch.bfloat16 
device = "cuda"

from unsloth import FastModel

model, _ = FastModel.from_pretrained(
    model_name = "unsloth/gemma-3n-E2B-it",
    dtype=None, 
    attn_implementation="eager",
    max_seq_length = 2048, # Choose any for long context!
    load_in_4bit = False,  # 4 bit quantization to reduce memory
    full_finetuning = False, # [NEW!] We have full finetuning now!
    device_map="cpu"
    # token = "hf_...", # use one if using gated models
)

from transformers import AutoTokenizer, set_seed

set_seed(99)

tokenizer = AutoTokenizer.from_pretrained(
        config.model_name,
        pad_token=ChatmlSpecialTokens.pad_token.value,
        additional_special_tokens=ChatmlSpecialTokens.list(),
    )

#from unsloth import add_new_tokens
#add_new_tokens(model, tokenizer.tokenizer, new_tokens=ChatmlSpecialTokens.list()); 
#tokenizer.pad_token = ChatmlSpecialTokens.pad_token.value

tokenizer.chat_template = (
    "{{ bos_token }}{% for message in messages %}{% if message['role'] != 'system' %}{{ '<start_of_turn>' + message['role'] + '\n' + message['content'] | trim + '<end_of_turn><eos>\n' }}{% endif %}{% endfor %}{% if add_generation_prompt %}{{'<start_of_turn>model\n'}}{% endif %}"
)

model.resize_token_embeddings(len(tokenizer))
model = model.to("cuda")
e
from datasets import load_dataset

def preprocess_and_filter(sample):
  """Preprocesses and filters a sample based on token length"""
  messages = sample["messages"]
  text = tokenizer.apply_chat_template(messages, tokenize=False)
  tokens = tokenizer.encode(text, truncation=False)

  if len(tokens) <= config.training_arguments["max_seq_length"]:
    return {"text": text}
  else:
    return None


dataset = (load_dataset(config.dataset_name, split="train")
        .rename_column("conversations", "messages")
        .map(preprocess_and_filter, remove_columns="messages")
        .filter(lambda x: x is not None, keep_in_memory=False)
    )

dataset_train = dataset.train_test_split(test_size=0.2, shuffle=True, seed=0)
dataset_test = load_dataset(config.dataset_name, split="test")

def generate_from_model_batch(batch_conversations, model, tokenizer):
  prompts = [tokenizer.apply_chat_template(conv, tokenize=False) for conv in batch_conversations]

  inputs = tokenizer(prompts,
                     return_tensors="pt",
                     padding=True,
                     truncation=True,
                     max_length=2048,
                     add_special_tokens=False).to(device)

  outputs = model.generate(
      **inputs,
      max_new_tokens=256,
      do_sample=True,
      top_p=0.95,
      temperature=0.01,
      repetition_penalty=1.0,
      eos_token_id=tokenizer.eos_token_id,
  )

  # Get lengths of prompts
  prompt_lengths = [len(tokenizer(prompt)["input_ids"]) for prompt in prompts]

  # Decode outputs, excluding the prompt portion
  generated_decoded = []
  for i, output in enumerate(outputs):
      generated = tokenizer.decode(output[prompt_lengths[i]:], skip_special_tokens=False)
      generated_decoded.append(generated.strip())

  return generated_decoded

def compute_matching_percentage(list1, list2):
    """Computes the percentage of matching elements between two lists."""
    if not list1 or not list2:
        return 0.0
    count1, count2 = Counter(list1), Counter(list2)
    matches = sum(min(count1[code], count2[code]) for code in count1 if code in count2)
    return matches / len(list2)


def find_longest_common_sequence_length(list1, list2):
    """Finds the length of the longest common contiguous sequence between two lists."""
    if not list1 or not list2:
        return 0
    m, n = len(list1), len(list2)
    prev_row = [0] * (n + 1)
    current_row = [0] * (n + 1)
    max_length = 0
    for i in range(1, m + 1):
        prev_row, current_row = current_row, prev_row
        for j in range(1, n + 1):
            if list1[i - 1] == list2[j - 1]:
                current_row[j] = prev_row[j - 1] + 1
                max_length = max(max_length, current_row[j])
            else:
                current_row[j] = 0
    return max_length

def evaluate_function_calling(dataset, model, tokenizer, batch_size=8):
    test_examples = len(dataset)
    tooling = []
    being_useful = []
    queries =  []
    answers = []

    for i in range(test_examples):
      conversations = []
      for item in dataset[i]["conversations"]:
          if item["role"] != "model":
              conversations.append(item)
          if item["role"] == "model":
              queries.append(conversations[:])
              answers.append(item["content"])
              conversations.append(item)

    batches = [queries[i:i + batch_size] for i in range(0, len(queries), batch_size)]
    generated = []
    for batch in tqdm(batches):
        generated.extend(generate_from_model_batch(batch, model, tokenizer))

    for ground_truth, generated in zip(answers, generated):
        ground_truth_tokens = tokenizer(ground_truth)["input_ids"]
        generated_tokens = tokenizer(generated)["input_ids"]

        # Evaluate function calling accuracy if tool call is present
        if "<tool_call>" in ground_truth:
            seq = find_longest_common_sequence_length(
                ground_truth_tokens, generated_tokens
            )
            matches = seq / len(ground_truth_tokens)
            tooling.append(matches)
        else:
            matches = compute_matching_percentage(
                ground_truth_tokens, generated_tokens
            )
            being_useful.append(matches)

    #torch.cuda.empty_cache()

    print(f"\nAccuracy in function calling: {np.mean(tooling):0.5f}")
    print(f"Match in helpful exchange: {np.mean(being_useful):0.5f}")



evaluate_function_calling(dataset_test.select(range(300)),
                          model,
                          tokenizer,
                          batch_size=config.batch_size)

exit()

from peft import LoraConfig, TaskType
from trl import SFTConfig, SFTTrainer

peft_config = LoraConfig(
        **config.lora_arguments,
        task_type=TaskType.CAUSAL_LM,
    )

training_arguments = SFTConfig(
    **config.training_arguments,
    output_dir=config.output_dir,
    fp16=config.fp16,
    bf16=config.bf16,
)

trainer = SFTTrainer(
    model=model,
    args=training_arguments,
    train_dataset=dataset_train["train"],
    eval_dataset=dataset_train["test"],
    processing_class=tokenizer,
    peft_config=peft_config,
)

model.config.use_cache = False
model.config.pretraining_tp = 1

trainer.train()





