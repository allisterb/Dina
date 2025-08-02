# Install latest transformers for Gemma 3N
# !pip install --no-deps transformers>=4.53.1 # Only for Gemma 3N
# !pip install --no-deps --upgrade timm # Only for Gemma 3N

#from huggingface_hub import login
#login()

from unsloth import FastVisionModel # FastLanguageModel for LLMs

import torch._dynamo 
torch._dynamo.config.cache_size_limit = 64  # or higher  


model, processor = FastVisionModel.from_pretrained(
    "unsloth/gemma-3n-E2B-it",
    load_in_4bit = True, # Use 4bit to reduce memory use. False for 16bit LoRA.
    use_gradient_checkpointing = "unsloth", # True or "unsloth" for long context
)

model = FastVisionModel.get_peft_model(
    model,
    finetune_vision_layers     = True, # False if not finetuning vision layers
    finetune_language_layers   = True, # False if not finetuning language layers
    finetune_attention_modules = True, # False if not finetuning attention layers
    finetune_mlp_modules       = True, # False if not finetuning MLP layers

    r = 16,                           # The larger, the higher the accuracy, but might overfit
    lora_alpha = 16,                  # Recommended alpha == r at least
    lora_dropout = 0,
    bias = "none",
    random_state = 3407,
    use_rslora = False,               # We support rank stabilized LoRA
    loftq_config = None,               # And LoftQ
    target_modules = "all-linear",    # Optional now! Can specify a list if needed
    modules_to_save=[
        "lm_head",
        "embed_tokens",
    ],
)

from datasets import load_dataset

dataset = load_dataset("unsloth/Radiology_mini", split="train")

instruction = "You are an expert radiologist. Describe accurately what you see in this image."

def convert_to_conversation(sample):
    conversation = [
        {
            "role": "user",
            "content": [
                {"type": "text", "text": instruction},
                {"type": "image", "image": sample["image"]},
            ],
        },
        {"role": "assistant", "content": [{"type": "text", "text": sample["caption"]}]},
    ]
    return {"messages": conversation}

converted_dataset = [convert_to_conversation(sample) for sample in dataset]

print(converted_dataset[20])

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