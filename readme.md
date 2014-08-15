TFS Diff Report
=======
This utility generates .diff files (in context diff format) for a given range of changesets in TFS.

#### Command line arguments
  
**/u** — URL of a TFS team project (e.g. "http://tfs.example.com:8080/TfsDiffReport");  
**/v** — Range of the changesets (e.g. 170~180) or a single changeset (e.g. 170);  
**/p** — (*optional*) Paths filter, only files under the specified server paths will be reported (e.g. "$/Trunk/Installer");  
**/e** — (*optional, default value = ".cs;.csproj;.cshtml;.js"*) Extension filter, only files with the specified extensions will be reported (e.g. ".html;.css"); 
  
Example: **tfsdiffreport /u "http://tfs.example.com:8080/TfsDiffReport" /v 800~900 /e ".txt"**