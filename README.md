# Github issue database

Fill out your LLM API details in the `api-settings.json` - I've included mine because I'm using local settings so there aren't any private tokens.

Copy the `github-settings-example.json` to `github-settings.json` and fill out the settings in there.

The LLM layer and conversation layer are seperated out, as well as the prompt strings - copy `LlmPrompts.resx.example` to `LlmPrompts.resx` and customise the strings to your environment.

Copy and paste this into the folder in a prompt:

```
cp github-settings-example.json github-settings.json
cp LlmPrompts.resx.example LlmPrompts.resx
```

It should be easy enough to just edit the resx strings, but the defaults should be fine too. I'd check mine in but I prime it with a bit of context about the issues that might not be appropriate in all environments.

Install Marqo https://github.com/marqo-ai/marqo - I'm just using the local docker image, I haven't added any authentication. If you fill out the `MarqoApiKey` it should work with their cloud environment.

Install LM Studio https://github.com/lmstudio-ai or use the OpenAi endpoint with an API key, or another compatible endpoint.

Run with `--refresh` to download, summarise, store and index all your github issues. I've made the issue downloader fairly modular however, so really you should be able to use this for any document store.

You can then search and ask questions about them. It's remarkably good.