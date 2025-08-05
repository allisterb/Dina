from datasets import load_dataset
dataset = load_dataset("pixparse/docvqa-single-page-questions", split="train")

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
print(convert_to_conversation(dataset[20]))