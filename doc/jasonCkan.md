

Grammar requirements checked or enforced.

``` 
<CkanFile>    := <CompoundValue> <EOF>

NOPE: <ListNV>~~        := <Empty> illegal for now
<ListNV>        := <NameValue>  <MoreListNV>

<MoreListNV>    := <Empty>
<MoreListNV>    := "," <EOL> <NameValue> <MoreListNV>

<NameValue>     := <NameString> ":" <Value> <EOL>

<Value>         := <ValueString>
<Value>         := <ValueNumber>
<Value>         := <CompundValue>
<Value>         := <ArrayValue>


<CompoundValue> := "{" <EOL> <ListNV> <EOL> "}"

<ArrayValue>    := "[" <EOL> <ValueList> <EOL> "]"


NOPE: <ValueList>     := <Empty> Illegal for now
<ValueList>     := <Value> 
<ValueList>     := <Value> <,> <EOL> <ValueList>  

<ValueString>   := <String>
<NameString>    := <AsciiString> 
<ValueNumber>   := <SuitableNumberDefn>

<EOL>           := "\r\n"  
                := "\n\r"  // NOT "normal" but "legal"
                := "\n"
                := "\r"  // NOT "normal" but "legal"

<EOF>           := <EOL> end of file.
                := end of file. 

<Empty>         :=

AsciiString     := <">  MoreAscii <">
MoreAscii       := <Upper>|<lower>|<Digits>|<_>|<->
<String>        := <"> <legalChars> <">
<legalChars>    := Printable Ascii Under 127
                := Printable chars Under 255
                := Multichar UTF8 as and when required.
```

