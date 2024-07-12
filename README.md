# Github issue database

Fill out your LLM API details in the `api-settings.json` - I've included mine because I'm using local settings so there aren't any private tokens. You can update the API keys as you see fit, it should work fine with an OpenAi endpoint.

Copy the `github-settings-example.json` to `github-settings.json` and fill out the settings in there.

The LLM layer and conversation layer are seperated out, as well as the prompt strings - copy `LlmPrompts.resx.example` to `LlmPrompts.resx` and customise the strings to your environment.

Copy and paste this into the folder in a prompt to copy the files if you're lazy and just want that done:

```
cp github-settings-example.json github-settings.json
cp LlmPrompts.resx.example LlmPrompts.resx
```

You can edit the resx strings in the XML with notepad or use an IDE, but the defaults should be fine too. I'd check mine in but I prime it with a bit of context about the issues that might not be appropriate in all environments.

Install Marqo https://github.com/marqo-ai/marqo - I'm just using the local docker image, I haven't added any authentication. If you fill out the `MarqoApiKey` it should work with their cloud environment.

To install docker on windows (this should all work on linux, I just haven't tested):

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

Run with `--refresh` to download, summarise, store and index all your github issues. I've made the issue downloader fairly modular however, so really you should be able to use this for any document store.

You can then search and ask questions about them. It's remarkably good.
