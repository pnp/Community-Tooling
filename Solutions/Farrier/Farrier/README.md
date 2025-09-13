# Farrier

Farrier is a rule runner, sample consolidator, and documentation generating command line tool. Wowee!

## Why Farrier?

Farrier started life as MDLooper with the intent of taking CSV files and generating markdown files. It's since evolved to use the same XML based configuration structure and internal expression language to also run custom inspection rules against directories, generate CSVs from disparate JSON files, and create grouped JSON files.

### Should you use it?

Sure? I guess?

It's really just a tool [Chris Kent](https://github.com/thechriskent) made to help organize the [PnP List Formatting sample repository](https://aka.ms/List-Formatting) clean and to help keep the documents site up to date.

But... it's not really documented, it's hard to understand, and relatively limited. Yay?

If you're still interested you can reach out to discuss, or just dig in to either the code (fun!) or more likely by looking at the existing configuration files in the Samples\ListFormatting directory.

### Why is this here?

Sharing is Caring

## Running it

Farrier is a .NET Core command line tool. It can be run on Mac or Windows (paths are normalized).

Open up the project in VS Code (use the C# extension for solution support). Then you can either build it and use it directly, or update the args in the primary Program.cs file and run it in debug. There are several commented out args that can demonstrate how it's used for List Formatting.

Each command and all options come with help. Just run with `--help` to learn more.

## Basic rundown of functions

### Inspect

Validates folders and files using a ruleset. You can get all crazy with this stuff if you want.

Here's what it looks like to run the inspection of list formatting rules using the LFSampleValidation.xml configuration file:
```
farrier inspect -c Samples/ListFormatting/LFSampleValidation.xml -r ValidateSamples -s /Users/chriskent/Code/pnp/List-Formatting/
```

We use the above to validate every sample against a huge amount of conditions including all the key files being present, folder structure matching, README contents/structure, JSON formatting, common JSON mistakes, and even metadata recommendation/validation.

The XML structure is defined and documented in Inspection.xsd. The basic idea is you create a rule with any number of sub rules. Those rules can contain things like tokens that will be swapped in and out as needed. Tokens can be defined directly, using expressions, or will be automaticaly determined (like `@@Each@@` when in loops). Rules can also have things like suppressions which are for known exceptions.

When running these, it's easier to run the inspection in batches by adjusting the `skip` and `limit` options on the key validation rules (near the bottom but above the suppressions). Just update those values accordingly before running.

### RoundUp

Extracts JSON values to CSV. It can also create a master JSON file at the same time.

Here's what it looks like to run the roundup of list formatting samples to create the master CSV (used in the forge below) by reading from each individual sample's sample.json and also creating the master JSON file samples.json:

```
farrier roundup -m Samples/ListFormatting/LFAssetMap.xml -s /Users/chriskent/Code/pnp/List-Formatting/ -j sample.json --overwrite --pathdepth 3 --joinedfilename samples.json -o /Users/chriskent/Code/pnp/Community-Tooling/Solutions/Farrier/Farrier/Farrier/Samples/ListFormatting -f LFSamples.csv
```

In the List Formatting repo, we've extended the sample.json concept to include a bunch of metadata about each sample. This metadata is used to create the groupings section of our documentation. This allows us to play nice with the overall Sample browser that goes across repos while also having extra details to make finding samples easier.

You can find details about the asset map XML configuration file using XML\Map.xsd. But the basic idea is you define columns for the CSV file based on JSON queries and can even apply transformation expressions on those values. Wowee!

### Forge

Generate files using a blueprint.

You can find the documentation and validation for forge blueprints in XML\Forge.xsd.

Blueprints contain file definitions for generation. Typically it's been used to create markdown files as seen in the Groupings section of the List Formatting docs site. But it can be used to generate any text file really. It was even used to generate key code files for flicon.io - wowee!

Files are composed of sections. Sections can have text in them or they can have a template. They can also have looping sections which are intended to use CSV files for input (the columns will be divided up into tokens) and then those loops can have items that use a template. Each item will be repeated per row. You can also configure loops to have queries and limits.

You can define templates as well. Templates are text that can reference other templates, tokens, and use expressions. Go nuts!

Here's what it looks like to generate key portions of the List Formatting docs site from a master CSV of samples (created using the roundup):
```
farrier forge -b Samples/ListFormatting/LFForgeBlueprint.xml --listtokens -o /Users/chriskent/Code/pnp/List-Formatting/docs/
```

### Other stuff

#### Copy File

There's a built in function to copy a file somewhere. It's a utility to make the whole process scriptable using the multi-command file approach work.

Here's an example of running it to move the master JSON file into the List Formatting repo:
```
farrier copyfile -f Samples/ListFormatting/samples.json -o /Users/chriskent/Code/pnp/List-Formatting/ --overwrite
```

#### From File

Commandline tools are cool and all, but there's a bunch of options and that can be both hard to remember and time-consuming to type each time. So, put each command on a single line in a text file and then use the `fromfile` option to run them. There's probably easier ways to script all of these, but this keeps things contained in this single project.

Here's what it looks like to run the key roundup and forge commands for List Formatting:
```
farrier fromfile -f Samples/ListFormatting/Farrier.txt
```

We run the inspections on their own (we have hundreds of samples and it's just easier to do that in batches). The inspections are critical for PRs and to ensure everything across the whole repo is in the expected shape. Then we run the above to generate the master CSV and JSON files then generate all the updated docs from them. Then we dance!

## Expressions

You write expressions by using the expression name (all uppercase starting with a $) and putting parenthesis around arguments. Arguments are separated with a `#,`. Anything not part of that is considered the content of those expressions. Everything is a string, but arguments or properties (XML) will convert those strings into values like numbers or booleans (`'true'`, `'false'`).

Here's the expressions that are available:

- `$ADD`: Adds 2 numbers together and returns the result
  - `$ADD(4,#8)` will result in `12`

- `$AND`: Returns `true` when all parameters are true and `false` otherwise (minimum of 2 parameters but can take an unlimited number)
  - `$AND(true,#true)` will result in `true`
  - `$AND(true,#false,#true)` will result in `false`
  - `$AND(false,#false,#false)` will result in `false`

- `$CONTAINS`: Returns `true` if the 1st parameter contains the 2nd parameter
  - `$CONTAINS(horse,#ho)` will result in `true`
  - `$CONTAINS(horse,#e)` will result in `true`
  - `$CONTAINS(horse,#squirrel)` will result in `false`

- `$DIRECTORYNAME`: Returns the name of the containing directory of a given path
  - `$DIRECTORYNAME(generic-row-actions/delete.json)` will result in `generic-row-actions`

- `$DIVIDE`: Divides the 1st number by the 2nd and returns the result
  - `$DIVIDE(4,#2)` will result in `2`

- `$ENDSWITH`: Returns `true` if the 1st parameter ends with the 2nd parameter
  - `$ENDSWITH(horse,#ho)` will result in `false`
  - `$ENDSWITH(horse,#e)` will result in `true`

- `$EQUALS`: Returns `true` when the 1st argument matches the 2nd and `false` otherwise
  - `$EQUALS(4,#4)` will result in `true`
  - `$EQUALS(horse,#cute)` will result in `false`

- `$FILEEXTENSION`: Returns the extension of the file of a given path
  - `$DIRECTORYNAME(generic-row-actions/delete.json)` will result in `json`

- `$FILENAME`: Returns the name of the file of a given path
  - `$DIRECTORYNAME(generic-row-actions/delete.json)` will result in `delete.json`

- `$FORMATDATE`: Formats the 1st parameter (a date/time string) using the .NET format specifiers for dates as the 2nd parameter
  - `$FORMATDATE(12/26/1981 4pm,#HH:mm:ss)` will result in `16:00:00`

- `$FORMATNUMBER`: Formats the 1st parameter (a number) using the .NET format specifiers for numbers as the 2nd paramter
  - `$FORMATNUMBER(0.45,#P)` will result in `45%`

- `$GT`: Returns `true` when the 1st number is greater than the 2nd and `false` otherwise
  - `$GT(5,#2)` will result in `true`
  - `$GT(5,#5)` will result in `false`
  - `$GT(2,#5)` will result in `false`

- `$GTE`: Returns `true` when the 1st number is greater than or equal to the 2nd and `false` otherwise
  - `$GTE(5,#2)` will result in `true`
  - `$GTE(5,#5)` will result in `true`
  - `$GTE(2,#5)` will result in `false`

- `$IF`: Returns the 2nd parameter when the first parameter is `true` and the 3rd parameter when `false`
  - `$IF(true,#horse,#cat)` will result in `horse`
  - `$IF(false,#horse,#cat)` will result in `cat`

- `$IN`: Returns `true` if the 1st parameter is equal to any of the other parameters (minimum of 2 parameters but can take an unlimited number)
  - `$IN(horse,#pig,#cat,#horse,#lizard)` will result in `true`
  - `$IN(rabbit,#pig,#cat,#horse,#lizard)` will result in `false`

- `$INDEXOF`: Returns the 0-based index of the start of the first occurrence of the 2nd string in the 1st string and -1 if not found
  - `$INDEXOF(old horse,#horse)` will result in `4`
  - `$INDEXOF(old horse,#o)` will result in `0`
  - `$INDEXOF(old horse,#cat)` will result in `-1`

- `$ISEMPTY`: Returns `true` if the contents are nothing or just whitespace and `false` otherwise
  - `$ISEMPTY()` will result in `true`
  - `$ISEMPTY(horse)` will result in `false`
  - `$ISEMPTY(  )` will result in `true`

- `$LASTINDEXOF`: Returns the 0-based index of the start of the last occurrence of the 2nd string in the 1st string and -1 if not found
  - `$LASTINDEXOF(old horse,#horse)` will result in `4`
  - `$LASTINDEXOF(old horse,#o)` will result in `5`
  - `$LASTINDEXOF(old horse,#cat)` will result in `-1`

- `$LENGTH`: Returns the number of characters of the string
  - `$LENGTH(horse)` will result in `5`

- `$LOWER`: Returns the lowercase version of the contents
  - `$LOWER(horsemeat yum)` will result in `horsemeat yum`
  - `$LOWER(horseMeat yuM)` will result in `horsemeat yum`
  - `$LOWER(HORSEMEAT YUM)` will result in `horsemeat yum`

- `$LT`: Returns `true` when the 1st number is less than the 2nd and `false` otherwise
  - `$LT(5,#2)` will result in `false`
  - `$LT(5,#5)` will result in `false`
  - `$LT(2,#5)` will result in `true`

- `$LTE`: Returns `true` when the 1st number is less than or equal to the 2nd and `false` otherwise
  - `$LTE(5,#2)` will result in `false`
  - `$LTE(5,#5)` will result in `true`
  - `$LTE(2,#5)` will result in `true`

- `$MOD`: Performs a modulus operation by providing the number of times the 1st number contains the 2nd evenly
  - `$MOD(4,#2)` will result in `2`
  - `$MOD(16,#3)` will result in `5`

- `$MULTIPLY`: Multiplies the first number by the 2nd and returns the result
  - `$MULTIPLY(4,#2)` will result in `8`

- `$NOT`: Returns `true` when you give it `false` and `false` when you give it `true`
  - `$NOT(true)` will result in `false`
  - `$NOT(false)` will result in `true`

- `$OR`: Returns `true` when any parameter is true and `false` if all parameters are `false` (minimum of 2 parameters but can take an unlimited number)
  - `$OR(true,#true)` will result in `true`
  - `$OR(true,#false,#true)` will result in `true`
  - `$OR(false,#false,#false)` will result in `false`

- `$PATH`: Normalizes paths for the system you're on so you can write `\` or `/` regardless

- `$PROPER`: Returns the TitleCase version of the contents using `en-US` culture rules
  - `$PROPER(horsemeat yum)` will result in `Horsemeat Yum`
  - `$PROPER(horseMeat yuM)` will result in `Horsemeat Yum`
  - `$PROPER(HORSEMEAT YUM)` will result in `Horsemeat Yum`

- `$REPLACE`: Returns the 1st parameter with every occurrence of the 2nd parameter replaced with the 3rd parameter
  - `$REPLACE(horses are horsetastic!,#horse,#squirrel)` will result in `squirrels are squirreltastic`

- `$RXESCAPE`: Returns the contents with regular expression characters escaped

- `$STARTSWITH`: Returns `true` if the 1st parameter starts with the 2nd parameter
  - `$STARTSWITH(horse,#ho)` will result in `true`
  - `$STARTSWITH(horse,#e)` will result in `false`

- `$SUBSTRING`: Returns the section of the 1st parameter (string) starting at the 0-based index of the 2nd parameter and using the 3rd parameter as the length
  - `$SUBSTRING(horse,#2,#2)` will result in `rs`
  - `$SUBSTRING(horse,#0,#3)` will result in `hor`

- `$SUBTRACT`: Subtracts the 2nd number from the 1st and returns the result
  - `$SUBTRACT(5,#1)` will result in `4`

- `$TRIM`: Removes whitespace from the beginning and end of the contents
  - `$TRIM( explosion)` will result in `explosion`
  - `$TRIM(neigh neigh )` will result in `neigh neigh`

- `$UPPER`: Returns the uppercase version of the contents
  - `$UPPER(horsemeat yum)` will result in `HORSEMEAT YUM`
  - `$UPPER(horseMeat yuM)` will result in `HORSEMEAT YUM`
  - `$UPPER(HORSEMEAT YUM)` will result in `HORSEMEAT YUM`

- `$WHEN`

### Escapes

If you want to have an opening parenthesis `(` as part of the value you type rather than the opening of an expression (typically not needed as expressions require a keyword from above) you can use `!{!`

If you want to have a closing parenthesis `)` as part of the value you type rather than the opening of an expression (more common) you can use `!}!`