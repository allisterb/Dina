# Dina
Dina is a local-only, open-souce, cross-platform document intelligence agent that allows blind users to effectively navigate, understand, and query electronic and printed documents like letters, receipts, expense reports, invoices, tax forms, employee handbooks, training manuals, and other types of structured business documents which visually impaired employees must work with during their day-to-day activities. 

Dina uses Google's Gemma 3n model to extract, understand, and analyze information from documents using [document intelligence](https://en.wikipedia.org/wiki/Document_AI) tasks like visual question answering and information extraction, and to implement agents to assist document querying and orchestrate workflows using tools that interface with business IT systems like email and databases. ina lets blind users scan documents using an attached desktop scanner, and navigate and query documents and files and emails etc. using natural language understanding, to quickly locate and extract needed information for input into automated interactions with a company’s business processes and workflows. 
### Requirements 
* .NET 8.0 or higher
* Ollama
 
Dina requires an Ollama server with the following models:
* `hf.co/allisterb/gemma3n_e4b_tools_test-GGUF`
* `nomic-embed-text`

You can pull this models by running the ollama pull command for both models e.g. `ollama pull hf.co/allisterb/gemma3n_e4b_tools_test-GGUF && ollama pull nomic-embed-text` from your command line.
## Building

* Clone the Dina repo from GitHub: `git clone https://github.com/allisterb/Dina.git --recurse-submodules`
* Run `build.cmd` or `./build` in the Dino repository root.

## Running
Enter `dina` or `./dina` in the repository root.