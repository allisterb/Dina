# Install latest transformers for Gemma 3N
# !pip install --no-deps transformers>=4.53.1 # Only for Gemma 3N
# !pip install --no-deps --upgrade timm # Only for Gemma 3N

#from huggingface_hub import login
#login()

import pytesseract
from unsloth import FastVisionModel, FastModel 
import random

import torch._dynamo 
torch._dynamo.config.cache_size_limit = 64  # or higher  

model, tokenizer = FastVisionModel.from_pretrained(
    model_name = "unsloth/gemma-3n-E4B-it",
    dtype = None, # None for auto detection
    max_seq_length = 4096, # Choose any for long context!
    load_in_4bit = True,  # 4 bit quantization to reduce memory
    full_finetuning = False, # [NEW!] We have full finetuning now!
    # token = "hf_...", # use one if using gated models
)

from datasets import load_dataset
invoices_dataset =load_dataset("katanaml-org/invoices-donut-data-v1", split="train")
vmrc_dataset = load_dataset("NTT-hil-insight/VisualMRC", split="train")

from unsloth import get_chat_template
processor = get_chat_template(
    tokenizer,
    "gemma-3n"
)

FastVisionModel.for_inference(model) 

def compare(dataset, sample_index, image_key, instruction, gt_key):
    
    sample = dataset[sample_index]
    image = sample[image_key]
    instruction = instruction

    print(f"Gemma 3n instruction '{instruction}'on OCR text input...")
    messages = [
        {
            "role": "user",
            "content": [{"type": "text", "text":pytesseract.image_to_string(image, config='--oem 1 --psm 1')}, {"type": "text", "text": instruction}],
        }
    ]

    inputs = tokenizer.apply_chat_template(
        messages,
        add_generation_prompt = True, # Must add for generation
        return_tensors = "pt",
        tokenize = True,
        return_dict = True,
    ).to("cuda")

    from transformers import TextStreamer

    text_streamer = TextStreamer(processor, skip_prompt=True)
    model.generate(**inputs, streamer = text_streamer, max_new_tokens = 256,
                            use_cache=True, temperature = 1.0, top_p = 0.95, top_k = 64)

    print("Gemma 3N on image input...")

    messages = [
       {
           "role": "user",
           "content": [{"type": "image"}, {"type": "text", "text": instruction}],
       }
    ]
    input_text = processor.apply_chat_template(messages, add_generation_prompt=True)

   # Convert grayscale image to RGB
    if image.mode == 'L':
       image = image.convert('RGB')

    inputs = processor(
       image,
       input_text,
       add_special_tokens=False,
       return_tensors="pt",
    ).to("cuda")


    text_streamer = TextStreamer(processor, skip_prompt=True)
    model.generate(**inputs, streamer = text_streamer, max_new_tokens = 256,
                           use_cache=True, temperature = 1.0, top_p = 0.95, top_k = 64)

    print("Dataset ground truth...")

    print(dataset[sample_index][gt_key])
    
#compare(invoices_dataset, 16, "image", "gt_parse", "Extract all information as JSON from this invoice.")

compare(vmrc_dataset, 16, "image", vmrc_dataset[16]["question"], "answer")
exit()

model = FastModel.get_peft_model(
    model,
    finetune_vision_layers     = False, # Turn off for just text!
    finetune_language_layers   = True,  # Should leave on!
    finetune_attention_modules = True,  # Attention good for GRPO
    finetune_mlp_modules       = True,  # SHould leave on always!

    r = 8,           # Larger = higher accuracy, but might overfit
    lora_alpha = 8,  # Recommended alpha == r at least
    lora_dropout = 0,
    bias = "none",
    random_state = 3407,
)

def convert_to_conversation(sample):
    conversation = [
        {
            "role": "user",
            "content": [
                {"type": "text", "text": sample['question']},
                {"type": "image", "image": sample["image"]},
            ],
        },
        {"role": "assistant", "content": [{"type": "text", "text": sample["answers"][0]}]},
    ]
    return {"messages": conversation}


converted_dataset = [convert_to_conversation(sample) for sample in dataset[:1000]]

from unsloth.trainer import UnslothVisionDataCollator
from trl import SFTTrainer, SFTConfig

FastVisionModel.for_training(model) # Enable for training!

trainer = SFTTrainer(
    model=model,
    train_dataset=converted_dataset,
    processing_class=processor.tokenizer,
    data_collator=UnslothVisionDataCollator(model, processor, resize=512),
    args = SFTConfig(
        per_device_train_batch_size = 1,
        gradient_accumulation_steps = 4,
        gradient_checkpointing = False,

        # use reentrant checkpointing
        # gradient_checkpointing_kwargs = {"use_reentrant": False},
        max_grad_norm = 0.3,              # max gradient norm based on QLoRA paper
        warmup_steps = 5,                 # Use when using max_steps
        max_steps = 60,
        # warmup_ratio = 0.03,
        # num_train_epochs = 2,           # Set this instead of max_steps for full training runs
        learning_rate = 2e-4,
        logging_steps = 1,
        save_strategy="steps",
        optim = "adamw_torch_fused",
        weight_decay = 0.01,
        lr_scheduler_type = "cosine",
        seed = 3407,
        output_dir = "outputs",
        report_to = "none",             # For Weights and Biases

        # You MUST put the below items for vision finetuning:
        remove_unused_columns = False,
        dataset_text_field = "",
        dataset_kwargs = {"skip_prepare_dataset": True},
        max_seq_length = 2048,
    )
)

# @title Show current memory stats
gpu_stats = torch.cuda.get_device_properties(0)
start_gpu_memory = round(torch.cuda.max_memory_reserved() / 1024 / 1024 / 1024, 3)
max_memory = round(gpu_stats.total_memory / 1024 / 1024 / 1024, 3)
print(f"GPU = {gpu_stats.name}. Max memory = {max_memory} GB.")
print(f"{start_gpu_memory} GB of memory reserved.")

trainer_stats = trainer.train()

# @title Show final memory and time stats
used_memory = round(torch.cuda.max_memory_reserved() / 1024 / 1024 / 1024, 3)
used_memory_for_lora = round(used_memory - start_gpu_memory, 3)
used_percentage = round(used_memory / max_memory * 100, 3)
lora_percentage = round(used_memory_for_lora / max_memory * 100, 3)
print(f"{trainer_stats.metrics['train_runtime']} seconds used for training.")
print(
    f"{round(trainer_stats.metrics['train_runtime']/60, 2)} minutes used for training."
)
print(f"Peak reserved memory = {used_memory} GB.")
print(f"Peak reserved memory for training = {used_memory_for_lora} GB.")
print(f"Peak reserved memory % of max memory = {used_percentage} %.")
print(f"Peak reserved memory for training % of max memory = {lora_percentage} %.")

FastVisionModel.for_inference(model)  # Enable for inference!

image = dataset[20]["image"]
instruction = "You are an expert radiologist. Describe accurately what you see in this image."

messages = [
    {
        "role": "user",
        "content": [{"type": "image"}, {"type": "text", "text": instruction}],
    }
]
input_text = processor.apply_chat_template(messages, add_generation_prompt=True)

# Convert grayscale image to RGB
if image.mode == 'L':
    image = image.convert('RGB')


inputs = processor(
    image,
    input_text,
    add_special_tokens=False,
    return_tensors="pt",
).to("cuda")

from transformers import TextStreamer

text_streamer = TextStreamer(processor, skip_prompt=True)
result = model.generate(**inputs, streamer = text_streamer, max_new_tokens = 256,
                        use_cache=True, temperature = 1.0, top_p = 0.95, top_k = 64)