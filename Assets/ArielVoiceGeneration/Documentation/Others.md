# Others

**[← Table of contents](../README.md#table-of-contents)**

### On this page

[Plugin project settings](#plugin-project-settings)<br/>
[Package a project](#package-a-project)<br/>
    *[No runtime generated sentences](#no-runtime-generated-sentences)*<br/>
    *[Blueprint only projects](#blueprint-only-projects)*<br/>
[Adding local models](#adding-local-models)<br/>

## Plugin project settings

Some parameters, such as the API Endpoint URL and the API key used to authenticate the request, are required by the API. You can get your personal API Key by [contacting us at](mailto:contact@xandimmersion.com).


## Package a project

The Ariel plugin works on packaged projects for Windows. Other operating systems may work as well, but we do not officially support them.

### No runtime generated sentences

If your project does **not** use runtime generated sentences, meaning all speech are pre-generated using *[Ariel Text-To-Speech (Remote)](../Documentation/API.md/#ariel-remote-class)* or *[Ariel Text-To-Speech (Local)](../Documentation/API.md/#ariel-local-class)* are never used outside the editor, then you can simply remove the Ariel plugin before packaging the project.

> [!CAUTION]
> If you try to package a project that uses Ariel Editor elements at runtime, the build package will fail.
