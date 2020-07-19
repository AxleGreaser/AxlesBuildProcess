# Using CKAN to mange mod deployment while developing Mods locally

**Target Audience**:  

This document is aimed at helping developers looking to develop and robustly CI and test mods in short development, automated, cycles, while still using *Ckan* to help manage the complexity.
These instructions both require and assume not insubstantial levels of coding expertise and experience. This is the proverbial 'Fine Manual' that you should read. Certain aspects of this process are considered non obvious and hence this document is here to help. The process described here is however largely Caveat Emptor. Writing code and testing it is not for the faint of heart, and this approach and level of automation will not be a suitable approach for everyone. If it does not meet your needs don't use it.

**Goal**:  
of these instructions is to help a developer make a system that still allows the use of *Ckan* to perform the complex task of managing manage all the mods on a machine across multiple installs, but also provide clear simple assurance (face validity) that a newly compiled mod is the one that actually now running with very little room for that process, once it is setup, to go wrong and the developer not notice.    

-----

## Step 1
*Goal*: make a local *Ckan* repository: using the following procedure.
### 1.1 Directory Structure
 **Create** a Directory Tree of the form:  `./Data/LocalModName1`  
 with one directory for each mod that you want to build locally.  
  
**Populate** the Data/LocalModName1 with one or more *Ckan* files modeled after the ones found in various public repositories.  
**Repeat** all the steps 1.1 - 1.9 for LocalModName2..n

### 1.2.0 make the following changes to each *.ckan file
Build ***CkanLocalise*** from source  
Set the environment variable  
*CKANLocaliser = "Authorname/Local"* or whichever string you want prepended to various `ckan` identifier values to identify it as a Localised private build of the mod.  
  
  Then Execute this  
`> CKANLocalise Localise Src.Ckan  Dest.Ckan PathToDownLoadFile`

`PathToDownLoadFile` is the actual **absolute** path to the file not a URI for it.Do not include File:/// at the start of the path, Start the path with a drive letter.

This completes steps 1.2.1 - 1.9 for you.  
With the addition that becuase it can, it computed the hash values for `SHA1` & `SHA256` and recorded the *download_size*  
Now: Visually Review the `Dest.ckan` for sensibility file then proceed to Step 2.

### 1.2.1 make the following changes to each *.ckan file
**Start** with copy of a suitable *Ckan* file (acquired from one of the repos zip file) and edit it  
So as to be sure your local *Ckan* file is not confusingly similar to any in the public repository. 
**Change** these fields  
```json  
{ ...
"identifier": "Lcl_ModnameRebuilt",  
"name"  : "Local Modname Rebuilt",  
"Abstract" : "Locally rebuilt and modified version of Modname."  
```  
**Set** these entries to suitable desired values  
```json  
{ ...
"version": "1.1.1.1",  
"ksp_version_min": "1.9.0",  
"ksp_version_max": "1.9.99",  
```

### 1.3 Change the version specification of the ckan file.
**Set** the version Number of the CKAN specification to a large enough eg 1.4 for the features of the specification that you are using
```json  
{  
    "spec_version": 1.4,  
    "identifier": "YourModsIdentifier",  
    ...
```
### 1.4 Allow Ckan to block installation of the incomapatible mods.
Ensure the specification of what the mod provides is a replacement of what the mod provided before you rebuilt it. But do mark down that the rebuilt mod **conflicts** with the original. 
```json  
{ ...
"provides": [ "Modname" ], 
"conflicts": [ { "name": "Modname" } ],
```
### 1.5
Both these entries \*can\* be removed which will simplify what needs to be updated after each rebuild.  
~~"download_size":  blah~~  
~~"download_hash": { blah blah    },~~  
### 1.7
The the last and really critical thing to get right is to modify the download entry 
```json
{...
"download": "file:///absolutePATH\On\YouLocalMachine.zip"
```
### 1.8 
Validate your `LocalModName1.ckan` File against the following json schema  
`https://github.com/KSP-CKAN/CKAN/blob/master/CKAN.schema`
``### 1.9
Now create a zip file localRepo.zip file of the entire local directory tree, including all the `./Data/LocalModNameX/LocalModNameX.ckan` files that you just made.   
Take Note of the absolute file path of that **localRepo.zip** file.
## Step 2
*Goal*: make certain that ckan is not using stale cached copies of recently built mod.
Either use the command line and do this
ckan repo add LocalRepoName file:///LocalAbsolutePathTo/LocalRepo.zip
or
use the GUI's settings to achieve the same end using the `RepoName|URI` syntax in settings. 

## Step 3
*Goal*: Validate/test what you have done so far.
Assuming you have a mod you have built locally or a tweak of one so it is at least recognisably different.
At this point you can test that what you have done so far worked.
The Mod "Local Modname Rebuilt" should now appear in the GUI and download provides and conflicts data should also all be there. Check those. Test the GUI behaves as you expect and allows and disallows the combinations of mods that you expect from your ckan file.

## Step 4
*Goal*: connect it into your build cycle procedures.  
As you go through development cycles of your mod you may want to test and retest incremental changes, or the output of additional logging statements when diagnosing bugs. You could do whatever it is *ckan* requires to automatically force these changes all the way through the system(bumping your versions build numbers). While there could be an automated process to keep tweaking the above process and making new ckan files for each new build.    

or ... 

You can use the ckan GUI to purge the cache for just the mod you select. Then remove it from the KSP install. Then select it and reinstall it. All that is made easier if you have also made your newly built mod a favorite and hence easy to find.   

or ...
  
You can use the following new command line features of *ckan* in a post built script.

Having just rebuilt a new mod and compressed it into a deployable zip file.
`ckan cache purge URI`  
Where URI is `file:///absolutePATH\On\YouLocalMachine.zip`  
That will purge just that file from ckans file cache.  

Then executing  
`ckan cache reinstall --headless -ksp KspInstallToUse LocalModName1`  
will remove the mod from that KSP install, if it is present, and then unconditionally install it again

Such a post build script will then every time when run ensure that the specified install of KSP runs using your just built MOD, while still allowing you the convenience of using CKAN to manage and visually verify what else is also running on that or other installs. 

