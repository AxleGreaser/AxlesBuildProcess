# AxlesBuildProcess
[Purposeful Design limitations of CKANLocaliser](#PurposefulDesignCKanLocaliser)

Definition of the Build process used by Axles repositories in [AxleGreaser(lnk)](https://github.com/AxleGreaser) 

CKanLocaliser will be a tool to help me make a local *ckan* repository using URIs of the form File:///****

Currently CKANLocaliser only validates files as fitting my requirements/assumptions. It specifically and on purpose by design chooses not to use std *json* file readers. It is both stricter and looser than those for design and function reasons.


 
## <a PurposefulDesignCKanLocaliser>Purposeful Design limitations of CKANLocaliser</a> 

First its name is spelled in English not USian. <small><small><small>(Yes I've seen the Oxford English dictionary entry for Localize it did not recolour my judgement.)</small></small></small>

Prohibited by design are things that would on visual inspection allow a *ckan* file to look Ok but actually not be. Such things include stronger restrictions on *whitespace*, where *json* has zero restrictions on whitespace. **Goal**: Basically if a *CkanLocaliser* valid file looks visually, to a human Okay, it is Okay. The are no sneaky name value pairs hiding at the ends of lines or anything. There are no weird **UTF8** chars that do things on your display you have never seen before.

I am still in the air on allowing arrays with a trailing comma and hence missing values. I don't like that. but. Seems like a common/frequent error.  EG: `"author": [AxleGreaser,]`  

Basically *CkanLocaliser* is coded with the if it is not required it is loudly denied philosophy. Where required is defined as required to validate *most* files in the current *ckan* repository. That it wont work with all of them is a good thing. Note: lots of missing EOLS (vs a netKan layout) or any "\\n" pairs of chars in file and then I and hence *CkanLocaliser* don't like it either. KISS. "\\n" renders as nothing in the current *ckan* **GUI** on my machine. KISS means it is thus both not required nor allowed in the *ckan* files *CkanLocaliser* agrees (because it thinks it understands them) operates on.  
**Rule**: invisible or possibly invisible, hence conceivably deceptive stuff is illegal. ASCII <127 >=32 is good + \n \r \t. (\x0B and \x0C but only under sufferance until I get grumpy and kill them too.)

### legal chars
As such lots of legal to *ckan* characters are currently outlawed for CKANlocaliser. It will be trivial to make them legal if that becomes a ***useful*** requirement. Changing them all (all n bytes of a UTF8) to # seems ... reasonable. # is visible.






