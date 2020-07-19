# Using CKANLocaliser as part of a build process

**Target Audience**:  

This document is aimed at helping developers looking to develop and robustly continuously improve and test mods in short development, automated, cycles, while still using *Ckan* to help manage the complexity.
These instructions both require and assume not insubstantial levels of coding expertise and experience. This is the proverbial 'Fine Manual' that you should read. Certain aspects of this process are considered non obvious and hence this document is here to help. The process described here is however largely Caveat Emptor. Writing code and testing it is not for the faint of heart, and this approach and level of automation will not be a suitable approach for everyone. If it does not meet your needs don't use it.

**Goal**:  
of these instructions is to help a developer make a system that still allows the use of *Ckan* to perform the complex task of managing manage all the mods on a machine across multiple installs, but also provide clear simple assurance (face validity) that a newly compiled mod is the one that actually now running with very little room for that process, once it is setup, to go wrong and the developer not notice.    

## First: do this: Build CkanLocaliser (link)

If that is considered too daunting then the things that can be done with it once built only get harder from here. There is thus no intention to produce a binary for CkanLocaliser. Build it locally or don't use it sre the reasonable options.

## Command line options

The scope for [options] exist.  
`--AlowEOLS ddd ddd` and two counts is one,  
`--AllowSlashN` specifigy the literal '\\n' Slash folowed by N no the 0xA itself is allowed.

## Validation
1/(of 2)  `> Ckanlocaliser **Validate** SrcFile`
*Validates one source file*.
It test some aspects of its validation as a  human readable ckan file. It in no sense at all validates the Json against the json schema. What it "evaluates" in some sense is readability. Deviation form netkan layout of whitespace(EOLs) is assessed and if too deviant rejected. Some files in the current ckan master repository are indeed rejected by design choice. it also applies a stricter test on what characters are legal in some cases than ckan does. 
However if a file passes validation, then it is expected that `> CkanLocaliser Localise Src Dst Zip` will both execute and produce valid results.  
It is still possible to have invalid ckan file pass our validation and be Localised. That too is by design.  
however if you have a valid ckan file say taken from the master set and it "Validates", then running ckan Localiser on it should also produce a valid ckan file. it should be a byte for byte copy of every thing that was not edited by the process.

2/(of 2)  `> Ckanlocaliser **Validate** SrcDirectory`
Recursively Scans the Directory (to depth 5) for every file (up to a hard limit of 100000) and every file with a name of the form *.ckan is validated. 
While pause functionality is in the code its currently always off. It may if you say Validate a directory containing the entire master repository produce a lot of output as it scans some 22000 files.

## Localise
1/(of 1)  `> Ckanlocaliser **Localise** SrcFile DestFile ZipFile`

This code  
0/ Writes this < `{ Localise error }` > to the DestFile  
This avoids, if possible, any possible confusion if the program throws and error that the Destfile was Localised.  
1/ reads and validates the SrcFile using the test described above.
Prints the usual diagnostic if that step fails.
2/ Validates that the zip file in fact exists, is an absolute file path, is readable, and it computes the **SHA1**, **SHA256**, and records the **download_size** of the zip File.
3/ It then localises the *ckan* File as per the description elsewhere. Example Localised files are available in the `/testData/LocalisedRegression` directory.
The localisation is done strictly by editing all other bytes in the file are untouched
4/. It then writes the file to DestFile

## VaporWare Coming real soon to an Application near you.
YAGNI:
Arrival time: shortly after they become needed.  
Until then **`YAGNI`**.
### FixHash
1/(of 1)  `> Ckanlocaliser **Fixhash** SrcFile DestFile ZipFile`  
Step 0/1/ As per Localise  

Does not do any localisation at all, or touch any other non specified bytes at all. 
What it would do is compute and fix **`sha1`** **`sha256`** and **`download_size`** then write the result to the DestFile.
It will add the hashes if absent.

### BumpVer
1/(of 1)  `> Ckanlocaliser **Bump** SrcFile DestFile ZipFile`  
Step 0/1/ As per Localise  

Does not do any localisation at all, or touch any other non specified bytes at all. 
Find and bump the last digits of the Version Number

and if the fields are present
What it would do is compute and fix **SHA1** **SHA256** and **download_size** then write the result to the DestFile.

1/(of 2)  `> Ckanlocaliser **Bump** SrcFile DestFile `
Like the above but will object, have Blitzed the DestFile if the Source file actually contains any of **sha1** **sha256** and **download_size**.

### Pretty
1/(of 2)  `> Ckanlocaliser **Fixhash** SrcFile DestFile ZipFile`  
Step 0/1/ As per Localise  

Does not do any localisation at all, or touch any other non specified bytes at all. The only bytes it does modify are...  
1/ (of 2) It removes <u>**all**</u> white-space then re-adds the white-space in the same pattern as NetKan, with indentation 4.
Thus pretty printing by making it conformant in layout with most other files people should be used to. Whether you or even I would prefer some of that bulk, single lined, is irrelevant conformance adds to reliability of human reading, interpretation, and validation of intent.

2/ (of 2) TBD: It either will or will not remove trailing commas in array statements.  Currently they are non canonical (present or absent at whim). Absent looks prettier (less half done) to me.

