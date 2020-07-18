# Test Data Description
This directory contains the test data.

## SrcCKan
This contains a number of ckan files with subtly varying syntaxes. it provides coverage testing exercising most or all functional execution pathways of Localise.   
But not all fault conditions and exceptions.

## LocalisedRegression
Contains literal versions of what the output of running `CkanLocaliser Regression` in the directory where it is built will produce. They are for humans to read.
The actual regression is done via SHA1 Hash codes in the source code of what the file is meant to contain.
It may even work across unix and windows systems as I believe the (\r\n or bare \n for EOL) is copied from the file it is working on.

## testOutput
Is the directory where CkanLocaliser Regression will generate its localised ckan files.

