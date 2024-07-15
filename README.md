# Github issue database

Fill out your LLM API details in the `api-settings.json` - I've included mine because I'm using local settings so there aren't any private tokens. You can update the API keys as you see fit, it should work fine with an OpenAi endpoint.

Copy the `github-settings-example.json` to `github-settings.json` and fill out the settings in there, or just run the application and it'll prompt you for your github settings.

The document downloader is an injected service so it should be very easy to swap for almost any type of document library.

The conversation and summary managers also are seperated out, as well as the prompt strings - this is designed to test different techniques and be easily extended.  

Default prompts are included in `prompts.json`, edit them as you see fit to get the best results for the type of documents you're summarising. Delete the file to restore the default prompts.

For the included database provider, use Marqo https://github.com/marqo-ai/marqo - I'm just using the local docker image, I haven't added any authentication. If you fill out the `DbApiKey` it should work with their cloud environment.

You can also add another implementation of `IVectorDb` and inject that, although currently it assumes the database handles creating the vectors. 

The console app should work just fine on linux, I haven't tested it yet however.

To install docker on windows:

```
winget install docker.dockerdesktop
```

You may have to restart.

To se up marqo via command prompt (from the github repo linked above):

```
docker rm -f marqo
docker pull marqoai/marqo:latest
docker run --name marqo -it -p 8882:8882 marqoai/marqo:latest
```

Install LM Studio https://github.com/lmstudio-ai or use the OpenAi endpoint with an API key, or another compatible endpoint. In LM studio just download the desired model, I found https://huggingface.co/legraphista/Gemma-2-9B-It-SPPO-Iter3-IMat-GGUF worked really well, and that's the model in the `api-settings.json` by default. Download that if you have ~12gb of video RAM, otherwise perhaps use an OpenAi API key.

Make sure you start the local server in LM Studio if that's what you're using.

Run with `--refresh` to download, summarise, store and index all your github issues. I've made the issue downloader fairly modular however, so really you should be able to use this for any document store.

You can then search and ask questions about them. It's remarkably good.

### Notes

Currently all documents are downloaded and summarised in a folder on your hard drive and then uploaded - I could just use the database but then swapping to a different database store would mean summarising all the documents again or connecting to both databases concurrently.

I've tried to break the application apart as much as possible so testing different connections and prompts is easy, I'm also planning to make it possible to select different LLMs for different purposes at a later stage.

### Example output

Demo using the public semantic-kernel repository from microsoft (chosen because it has around 2700 issues, which is enough to search but not enough to melt my computer summarising!)

```
Run with --refresh to refresh issues, summarise and index, add --reindex to reindex existing summaries

Loaded GithubSettings configuration from C:\software\LocalEmbeddings\github-settings.json
Loaded Prompts configuration from C:\software\LocalEmbeddings\prompts.json
Loaded ApiSettings configuration from C:\software\LocalEmbeddings\api-settings.json
Index initialized successfully.
Marqo server status:
Backend: Memory 1.34%, Storage 8.68%

Current Marqo indexes:
app-issues-summarised
semantic-kernel-issues

Current index: semantic-kernel-issues
Documents: 2765, Vectors: 11149

Search the issue database or hit enter to close:
chroma database bug

Searching, please wait..



  ## Top matches: ##

1: 1289: Chroma memory resets when store is initialized
2: 2700: Support full API in Chroma client
3: 403: Support for Chroma embedding database
4: 3046: Python: Cannot connect to remote Chroma DB
5: 2049: Chroma .NET MemoryRecordMetadata field 'is_reference' is boolean but is saved as number in ChromaDB
6: 2050: Python: Update Chroma Connector for Python SK from 0.3.29 -> 0.4.0
7: 1535: KeyError: 'is_reference' running search on Chroma
8: 2078: .Net: minRelevanceScore is unusable for Chroma's default distance function

* Press N to view the next page
* Press S to display a summary of these issues
Any other key to to start a new search

Summarising, please wait..
Here's a summary of the GitHub issues, highlighting their relation to each other and the "chroma database bug" search
theme:
* Chroma Memory Reset on Store Initialization: Bug in semantic_kernel where ChromaMemoryStore fails to load
  persisted memories during initialization, leading to data loss upon restarts.
* Full Chroma API Support in Chroma Client: Feature request for the Chroma client library to implement missing
  functionalities like embedding creation and metadata-based search, enhancing its capabilities.
* Request: Microsoft Library Support for Chroma: Feature request to integrate Chroma embedding database into a
  Microsoft LLM usage library, enabling seamless integration of external knowledge within applications.
* Chroma Memory Store Connection Issues: Bug in semantic_kernel where outdated connection methods used with
  ChromaMemoryStore cause failures when connecting to recent versions of chromadb.
* Chroma .NET MemoryRecordMetadata 'is_reference' Type Mismatch: Bug in Chroma .NET library's SQLite backend
  causing the boolean field is_reference to be stored as an integer, leading to deserialization errors during
  retrieval.
* Chroma Connector Python Update Issue: Major update to the Python connector (v0.4.0) involving simplified client
  setup, data store migration away from DuckDB/ClickHouse, and a new migration tool. Requires user action to
  update data schema.
* KeyError in Chroma Memory Store Search: Issue where ChromaMemoryStore raises a KeyError when searching external
  Chroma DBs (created outside Semantic Kernel) due to missing "is_reference" metadata in document records.
* .Net: minRelevanceScore unusable for Chroma's default distance function: Bug in .Net connector where
  minRelevanceScore parameter doesn't function correctly when using Chroma's default "l2" distance function,
  effectively disabling its filtering functionality.
Relation to "chroma database bug" search:
All these issues relate to bugs or desired improvements within the Chroma database ecosystem, directly impacting its
functionality, stability, and usability across different libraries and programming languages (Python, .NET). They
collectively highlight common areas needing attention and potential fixes within the Chroma project.

* Press Q to ask a question about the summary
* Press N to ask a question about the summary in a new conversation
Any other key to continue looking through the search results



  ## Top matches: ##

1: 1289: Chroma memory resets when store is initialized
2: 2700: Support full API in Chroma client
3: 403: Support for Chroma embedding database
4: 3046: Python: Cannot connect to remote Chroma DB
5: 2049: Chroma .NET MemoryRecordMetadata field 'is_reference' is boolean but is saved as number in ChromaDB
6: 2050: Python: Update Chroma Connector for Python SK from 0.3.29 -> 0.4.0
7: 1535: KeyError: 'is_reference' running search on Chroma
8: 2078: .Net: minRelevanceScore is unusable for Chroma's default distance function

* Press N to view the next page
* Press S to display a summary of these issues
Any other key to to start a new search


  ## .Net: minRelevanceScore is unusable for Chroma's default distance function ##


  ## Summary of GitHub Issue: .Net: minRelevanceScore unusable for Chroma's default distance function ##

Affected Parts:
* Microsoft Semantic Kernel .NET library
* Connectors/Connectors.Memory.Chroma/ChromaMemoryStore.cs file specifically
Bug Description:
The minRelevanceScore parameter provided in the .Net Chroma connector implementation for searching text embeddings is
currently unusable when using Chroma's default distance function, "l2".  This is because:
1. The current implementation assumes similarity scores fall between 0 and 1, which is not true for "l2" distance
   (which produces distances in the thousands).
2. Consequently, setting minRelevanceScore above 0 with "l2" results in no matches being returned, effectively
   rendering the parameter useless.
Workaround:
The user currently bypasses this issue by directly using QueryEmbeddingsAsync from the ChromaClient instead of relying
on the .Net connector's search functionality.
This bug essentially prevents utilizing the minRelevanceScore filtering feature when employing Chroma's default
distance function, limiting its usefulness in fine-tuning search results based on relevance.

Location: https://github.com/microsoft/semantic-kernel/issues/2078

* Press Q to ask a question about the current issue
* Press N to ask a question about the current issue in a new conversation
* Press R to search for related issues
* Press C to continue searching issues

Ask a question about this issue, enter to return:
what is the cause of the bug
Querying, please wait..

The bug stems from the ChromaMemoryStore.cs implementation in the .Net connector. It assumes similarity scores fall
between 0 and 1, but the default "l2" distance function returns distances in the thousands, making the
minRelevanceScore parameter unusable.


Ask another question or enter to return:
how is the user working around the issue
Querying, please wait..

The user is working around the issue by directly using the QueryEmbeddingsAsync method from the ChromaClient instead
of relying on the .Net connector's SearchAsync method.


Ask another question or enter to return:

```

### Note to self

You've used `git update-index --assume-unchanged prompts.json` so changes won't get committed. Use `git update-index --no-assume-unchanged prompts.json` if you want to change the example for others, especially if you add additional prompts!
