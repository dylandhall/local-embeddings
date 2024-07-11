# Github issue database

Fill out your LLM API details in the `api-settings.json` - I've included mine because I'm using local settings so there aren't any private tokens.

Copy the `github-settings-example.json` to `github-settings.json` and fill out the settings in there.

Install Marqo https://github.com/marqo-ai/marqo - I'm just using the local docker image, I haven't added any authentication.

Install LM Studio https://github.com/lmstudio-ai or use the OpenAi endpoint with an API key, or another compatible endpoint.

Run with `--refresh` to download, summarise, store and index all your github issues.

You can then search and ask questions about them.

### Example output

```
Run with --refresh to refresh issues, summarise and index, add --reindex to reindex existing summaries

Marqo server status:
Backend: Memory 1.01%, Storage 8.67%

Current Marqo indexes:
app-issues-summarised
app-issues
app-issues-new

Current index: app-issues-summarised
Documents: 8501, Vectors: 46495

Search the issue database:
module selection break into three lists for senior
Querying..


Top matches:
1: 14094: Update module selection page to show three different lists based on topic/senior readiness
2: 14063: Modules in senior readiness lists are getting added for schools who have switched every module off.
3: 13044: Update start module method to use the new method to check a module is allowed to be started
4: 14092: Add search function to module selection page
5: 13036: Method to take the targets lists for the school's state and combine with the school's inclusions and exclusions
6: 13029: Add remove module edit functionality to the new list page
7: 14095: Add module path dialog to senior mode
8: 14086: Add module selection page, and button to start a module on homepage

Hit S to display a summary of these issues, or N to display the next page of results
Any other key to start a new search
1
Update module selection page to show three different lists based on topic/senior readiness

Date created: 2023-08-11

Summary of Github Issue: Module Selection Page Redesign for Senior Students

Affected Parts:

* Backend: `SeniorWorkManager`, `ModuleChoiceManager`, `CourseManager`
* Frontend: Module selection page UI

Changes:
This issue restructures the module selection page for senior students, moving away from a single list to three distinct tabs based on school-defined senior readiness goals.

Key Changes:
1. Backend Logic Updates:
2. Frontend Tabbed Display:
3. Search Functionality Enhancement:
4. Topic Filtering (Checkbox):
5. Module Start Restrictions:
6. Error Handling & Testing:

This redesign aims to provide a clearer and more structured module selection experience for senior students, guiding them towards appropriate learning pathways based on their readiness goals.



Issue location https://github.com/mathspathway/app/issues/14094

Any key to return, Q to ask questions about the issue, or R to find related issues
Ask a question about this issue, enter to return:
does the standard list include topic modules

Querying, please wait..

The issue proposes creating three lists of modules on the module selection page:

* Standard
* Advanced
* Extension

These lists will be based on the school's senior readiness goals and topic.  The mockup provided shows these as tabs.



Keep asking questions or hit enter to return
what exactly is included in the lowest list

Querying, please wait..

According to the issue description, the "Standard" tab (the lowest list) will include:

* All modules from the general mathematics senior readiness goal and their prerequisites.
* Any modules in a topic the student has active, plus their prerequisites.



Keep asking questions or hit enter to return

```