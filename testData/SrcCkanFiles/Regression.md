# What the Regression tests
What cases does it cover  
`> CkanLocaliser Regression'


## Changed Values
many values have the value field replaced or modified.
Simple process to provide coverage little variation.
**"identifier":**, **"name":**, **"abstract":** all have localisation text prepended to value.
**"resource": {name:value}**'s are all deleted to default nonsensical values.
download is set to the specified value in regression its a nonsensical fixed value when actually localising it is set to a command line value and validated as existing.
**"download_size"** and hashes **":"SHA1"** **"SHA256"** are likewise set to default (9)whitebox) values for regression tests and actual observed values for actually localising.
The Code thus not tested due to the white box regression testing is simple (works or doesn't) and tested by observation that it [`> CkanLocaliser Localise src dst zip `]  works at all in the field.


## Author
No array syntax (in *actual.ckan)  
<= `"author" : "Someone" <EOL>`  
changed to  
=> `"author" : [ <EOL> "AxleGreaser", <EOL> "Someone" <EOL>`

Array syntax (in *11.ckan)  
<= `"author" : [ <EOL> "Someone" <EOL> <]> <,> <EOL>`  
changed to  
=> `"author" : [ <EOL> "AxleGreaser", <EOL> "Someone" <EOL> <]><,> <EOL>`  

Array syntax (with missing array value after comma) (in *12.ckan) 
<= `"author" : [ <EOL> <"Someone"> <,> <EOL> <]><,> <EOL>`  
changed to  
=> `"author" : [ <EOL> <"AxleGreaser"><,> <EOL> <"Someone"> <,> <EOL> <]><,> <EOL>`


## Provides
"provides" is missing (in *actual.ckan)  
<= `<EOF>`  
changed to  (inserted after the now required "license")
=> `"provides" : [ <EOL> <"FMRSContinued"> <EOL> <]> <,> <EOL>`

No array syntax (in *14.ckan)  
<= `"provides" : <"FMRSContinued"> <,> <EOL>`  
NOT changed  at all  
=> `"provides" : <"FMRSContinued"> <,> <EOL>`  

Array syntax (non empty with missing array value after comma) (in *12.ckan) 
<= `"provides" : [ <EOL> <"FMRSContinued"> <,> <EOL> <]><,> <EOL>`  
NOT changed  at all  
=> `"provides" : [ <EOL> <"FMRSContinued"><,>  <]><,> <EOL>`

Array syntax (with missing array value after comma) (in *13.ckan) 
<= `"provides" : [ <EOL> <"otherStuff"> <,> <EOL> <]><,> <EOL>`  
changed  to  
=> `"provides" : [ <EOL> <"FMRSContinued"><,> <EOL> <"otherStuff"> <,> <EOL> <]><,> <EOL>`


Array syntax (other order) (in *11.ckan) 
<= `"provides" : [ <EOL> <"otherStuff"> <,> <EOL> <"FMRSContinued"> <,> <EOL> <]><,> <EOL>`  
changed to  
=> `"provides" : [ <EOL> <"otherStuff"><,> <EOL> <"FMRSContinued"> <,> <EOL> <]><,> <EOL>`

## Conflicts
has potential difficulties as what in any existing one is potentially complexish...

"conflict" is missing (in *actual.ckan)  
<= `<EOF>`  
changed to  (inserted after the now required "license")  
=> `"conflicts":[{"name":"FMRSContinued"}],` (but pretty printed)

A mix of valid conflicts statements (in *11.ckan)  
<= ``` "conflicts":[ {"name":"FooBar"},  
				{ "name":"FuBar", "version":"1.1.1.1" } ],```  
changed to  
=> ``` "conflicts":[ {"name":"FMRSContinued"},{"name":"FooBar"},  
				{ "name":"FuBar", "version":"1.1.1.1" } ],```  

Same mix of valid conflicts statements but with valid conflict already present (in *12.ckan)  
<= ``` "conflicts":[ {"name":"FMRSContinued"},{"name":"FooBar"},  
				{ "name":"FuBar", "version":"1.1.1.1" } ],```  
NOT changed  at all  
=> ``` "conflicts":[ {"name":"FMRSContinued"},{"name":"FooBar"},  
				{ "name":"FuBar", "version":"1.1.1.1" } ],```  

Smaller mix of valid conflicts statements but with valid conflict already present (in *13.ckan)  
<= ``` "conflicts":[ {"name":"FMRSContinued"},  
				{ "name":"FuBar", "version":"1.1.1.1" } ],```  
NOT changed  at all  
=> ``` "conflicts":[ {"name":"FMRSContinued"},{"name":"FooBar"},  
				{ "name":"FuBar", "version":"1.1.1.1" } ],```  


"conflict" is missing as it was in *actual.ckan (in *14.ckan)  
<=> see above:
