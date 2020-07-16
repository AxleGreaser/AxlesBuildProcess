To Do

Does the file IO accept what range of UTF8 and then faithfully echo it as the exact same bytes.

Similarly how faithful is it with various /r /n /r /r /n /n patterns?
Also what is a new line anyway.


Currently every file in the ckan repository passes the test.

Real soon now it will stop doing that and reject quite few of them. They are all valid JSOn file but some have a hand crafted feel.
One has two name value pairs on one line.
Real Soon now the validator will reject the one that are visually dissimilar to standard NetKan formatted ones. (unless override command switches say otherwise.)


