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

Index initialized successfully.
Marqo server status:
Backend: Memory 1.73%, Storage 8.67%

Current Marqo indexes:
app-issues-summarised
app-issues
app-issues-new

Current index: app-issues-summarised
Documents: 8507, Vectors: 46526

Search the issue database or hit enter to close:
student unable to log in

Searching, please wait..


                                                          
  ## Top matches: ##                                      
                                                          
1: 8673: Students unable to login                         
2: 2837: Student's aren't able to login                   
3: 4959: Student login not working                        
4: 5532: Student can't log in, isn't on class login page
5: 5463: Teacher unable to log in, is instantly logged out
6: 1012: Students with Year Level 11 or 12 can't log in
7: 11364:  Cannot login to any teacher account
8: 3257: Student not showing up in class login page

* Press N to view the next page
* Press S to display a summary of these issues
Any other key to to start a new search

                                                                                                                                                                                                                                                                                                                                           
  ## Students unable to login ## 

Date created: 2020-09-14

  ## Issue Summary: Student Logout After Login ## 

Affected Systems: Student-Web, students-timeline
Changes:
This bug report describes an issue where students successfully log in using either username/password or watto authentication but are immediately logged out after attempting to access their timelines via the GetStudentTimelines call.  The expected behavior is for students to be presented with their timeline after successful login.
The issue needs to be resolved by identifying and fixing the code causing the unexpected logout following the GetStudentTimelines call.

* Press Q to ask a question about the current issue
* Press N to ask a question about the current issue in a new conversation
* Press R to search for related issues
* Press C to continue searching issues



  ## Top matches: ## 

1: 8673: Students unable to login
2: 2837: Student's aren't able to login
3: 4959: Student login not working
4: 5532: Student can't log in, isn't on class login page
5: 5463: Teacher unable to log in, is instantly logged out
6: 1012: Students with Year Level 11 or 12 can't log in
7: 11364:  Cannot login to any teacher account
8: 3257: Student not showing up in class login page

* Press N to view the next page
* Press S to display a summary of these issues
Any other key to to start a new search

Summarising, please wait..
Here's a summary of the issues, focusing on their relevance to a "student unable to log in" search:                                                                                                                                                                                                                
* Student Logout After Login (2020-09-14): Students log in successfully but are immediately logged out after attempting to access their timelines.  Relates to search: This is a direct bug causing login failure, though it occurs after the initial login succeeds.
* Student Login Failure (2017-09-13): Year 9 students at Specific School experience a login loop where the loading icon persists. Relates to search:  A classic login failure bug, preventing access entirely.
* Student Login Not Working (2018-10-04): The student login button in Teacher-Web redirects to an incorrect URL instead of authenticating.  Relates to search: Broken login functionality, preventing students from starting the process.
* Student Login Inconsistency (2019-02-05): Person's account redirects to the class login page instead of her dashboard. Relates to search: Data inconsistency causing a failed login experience.
* Teacher Instant Logout (2019-01-25): Specific Person is immediately logged out regardless of login method.  Not directly related: Focuses on teacher login, not student.
* Students with Year Level 11 or 12 can't log in (2016-03-04):  Year 11 and 12 students cannot log in due to a missing year level in a function handling module counts. Relates to search: Specific bug preventing login for certain student groups.
* Teacher Login Failure (2022-05-16): Teachers cannot log in through Watto or the web interface due to a 502 error from the Token API.  Not directly related: Focuses on teacher login, not student.
* Student Missing from Class Login Page (2017-11-21): Specific Person was temporarily missing from the class login page but resolved itself. Relates to search: Shows a scenario where a student appeared absent from the login process, though temporary.
Overall Relationship to Search:
Many of these issues directly relate to students being unable to log in due to bugs or configuration problems.  The "Student Logout After Login" issue is particularly relevant as it describes a successful login followed by immediate logout, which could be misinterpreted as an inability to log in initially.

* Press Q to ask a question about the summary
* Press N to ask a question about the summary in a new conversation
Any other key to continue looking through the search results

Ask question or enter to return:
are there any common causes
Querying, please wait..

Based on the provided issues, common causes for a student being unable to log in appear to be:                                                                                                                                                                                         
* Bugs in the login process itself:  This includes issues like the loading icon persisting (Student Login Failure), incorrect redirects (Student Login Not Working), and missing year levels preventing login for certain groups (Students with Year Level 11 or 12 can't log in).
* Data inconsistency: The "Student Login Inconsistency" issue highlights how incorrect data can lead to a failed login experience.
Let me know if you need further analysis on any specific issue.


Ask another question or enter to return:


```