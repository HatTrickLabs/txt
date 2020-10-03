### Basic Usage:
```c#
var fullName = new { FirstName = "John", LastName = "Doe"};

string template = "Hello {FirstName} {LastName}, this is just a test.";

TemplateEngine ngin = new TemplateEngine(template);

string result = ngin.Merge(fullName);

//result = Hello John Doe, this is just a test.
```


### Simple Tags
In its simplest form, the template engine can be used to inject data into text templates via *{tag}* replacement.

##### Data:
```c#
var fullName = new { FirstName = "John", LastName = "Doe"};
```
##### Template:
```
Hello {FirstName} {LastName}, this is just a test.
```

##### Result:
```
Hello John Doe, this is just a test.
```

##### Notes:
- The engine uses single brackets for tags.
- If a template contains any non-tag brackets, they can be escaped by doubling them up. {{ abc }} will render { abc } into the output.


### Simple Tags with Compound Expressions
Simple *{tag}s* can contain compound bind expressions to reference data from nested object structures.

##### Data:
```c#
var person = new 
{ 
	Name = new { First = "John", Last = "Doe"}, 
	Address = new 
	{ 
		Line1 = "123 Main St.", 
		Line2 = "Suite 100", 
		City = "Dallas", 
		State = "TX", 
		Zip = "77777" 
	} 
};
```

##### Template:
```
Hello {Name.First}, we see you currently live in {Address.City}, {Address.State}.
```

##### Result: 
```
Hello John, we see you currently live in Dallas, TX.
```


### Conditional Blocks:
The *{#if}* tag allows for conditionally rendering template blocks based on evaluation of *truthy/falsy* conditions.

##### Data:
```c#
var person = new 
{ 
	IsEmployed = true, 
	Employer = "Hat Trick Labs"
	Name = new { First = "John", Last = "Doe"}, 
};
```

##### Template:
```
Hello {Name.First} {Name.Last},
{#if IsEmployed}
We see you are currently employed at {Employer}.
{/if}
{#if !IsEmployed}
We see you are currently unemployed.
{/if}
```

##### Result:
```
Hello John Doe,
We see you are currently employed at Hat Trick Labs.
```

##### Notes: 
- The second if block is negated with the ! logic negation operator.
- Conditional blocks are not rendered for falsey values.  *Falsey* values include:
	* false boolean
	* null
	* numeric zero
	* empty string
	* empty collection
	* DBNull.Value
- Missing values are not considered *Falsey*.  An expression that attempts to bind a non-existant property|field|dictionary entry from the bound object will throw an exception.


### Iteration Blocks
The *{#each}* tag allows for conditional rendering based on collection types.  *{#each}* tags iterate over items in the provided
collection and render the contained text block.  The contained text block operates within the scope context of the iterated item.

##### Data:
```c#
var person = new 
{ 
	Employer = "Hat Trick Labs",
	Certifications = new[] { "mcse", "mcitp", "mcts" },
	Name = new { First = "John", Last = "Doe"}, 
};
```

##### Template:
```
Hello {Name.First} {Name.Last},

{#if Certifications}
We see you currently hold the following certs:
  {#each Certifications}
  - {$}
  {/each}
{/if}
{#if !Certifications}
  - We see you currently do not have ANY certs...
{/if}
```

##### Result:
```
Hello John Doe,

We see you currently hold  the following certs:
  - mcse
  - mcitp
  - mcts
```

##### Notes:
- An each block bound to a *falsy* value (null or empty) will result in no block content rendered.
- *{#each}* tags work on any object that implements the *System.Collections.IEnumerable* interface.
- The $ reserved varible always references the root value of local scope (*this*).  $ can be used in any tag within a template.
- the ..\ operator can be used to walk the scope chain.


### Variable Declaration and Usage
The variable declaration tag is used to declare and store a local variable.  *{?var:xyz=$.Name}* declares a local variable named xyz and sets it's value to $.Name *(this.Name)*.

##### Data:
```c#
var dbModel = new 
{ 
	Schema = "dbo",
	Tables = new[] 
	{
		new 
		{ 
			Name = "Person",
			Columns = new[]
			{
				new { Name = "Id", DataType = "int" },
				new { Name = "FirstName", DataType = "varchar(32)" },
				new { Name = "LastName", DataType = "varchar(32)" },
				new { Name = "BirthDate", DataType = "date" }
			}
		}
	}
};
```

##### Template:
```
{?var:schemaName = Schema}
Fields:
{#each Tables}
{?var:tableName = Name}
	{#each Columns}
[{:schemaName}].[{:tableName}].[{Name}] ({DataType})
	{/each}
{/each}
```

##### Result:
```
Fields:
[dbo].[Person].[Id] int
[dbo].[Person].[FirstName] varchar(32)
[dbo].[Person].[LastName] varchar(32)
[dbo].[Person].[Birthdate] date
```
##### Notes:
- Both declaring and referencing a variable requires the variable name be proceeded by a colon:
	* Declaration: *{?var:myVar = $ }*
	* Usage: *{:myVar} )*
- The colon ensures no collisions between declared variable names and properties, fields or keys of the bound object.
- Variables can be set via string literals, numeric literals, a value from the bound object, lamba expressions or boolean *true/false*:
	* String Literal: *{?var:someText = "Hello"}*
	* Numeric Literal: *{?var:someNum = 3.0d}*
	* Bound Expression: *{?var:someVal = $.SomeProperty}*
	* Lambda: *{?var:someVal = () => GetSomeValue}*
	* Boolean: *{var:isValid = true}*
- String literal values can be wrapped in double quotes or single quotes.
- Numeric literal values cannot be inferred and must contain a type suffix.  Valid type suffix values (case insensitive):
	* d - double
	* i - int
	* f - float/single
	* m - decimal
	* l - long


### Partial Template Blocks
The partial template *{>tag}* is used to inject sub template content.  

##### Data:
```c#
var attendees = new
{ 
	People = new []
	{
		{ Id = 1, FirstName = "John", LastName = "Doe"},
		{ Id = 2, FirstName = "John", LastName = "Doe"},
		{ Id = 3, FirstName = "Jane", LastName = "Smith"}
	},
	RsvpFormat = "<li><bold>{$.Id}</bold> - {$.LastName}, {$.FirstName}</li>"
}
```

##### Template:
```
<ul>
	{#each People}	
	{>RsvpFormat}
	{/each}
</ul>
```

##### Result:
```
<ul>
	<li><bold>1</bold> - Doe, John</li>
	<li><bold>2</bold> - Doe, John</li>
	<li><bold>3</bold> - Smith, Jane</li>
</ul>
```


### With Blocks
The *{#with}* template tag allows for a shift of local scope to a different position in the bound object.

##### Data:
```c#
var account = new 
{ 
	Person  = new 
	{
		Name = new { First = "John", Last = "Dow" },
		Address = new
		{
			Line1 = "112 Main St.",
			Line2 = "Suite 210",
			City = "Plano",
			State = "TX",
			Zip = "75075"
		},
		Employer = "Hat Trick Labs",
	},
};
```

##### Template
```
<div>Active Account:</div>
{#with Person.Name}
<div>{First} {Last}</div>
{/with}
<div>Address:</div>
{#with Person.Address}
<div>{Line1}{#if Line2}</br>{Line2}{/if}</div>
<div>{City}, {State} {Zip}</div>
{/with}
```

##### Results
```
<div>Active Account:</div>
<div>John Doe</div>
<div>Address:</div>
<div>112 Main St.</br>Suite 210</div>
<div>Plano, TX 75075</div>
```

##### Notes:
- Utilizing *{#with}* tags can help decrease template noise.  Rendering the address portion of the above example WITHOUT the *{#with}* tag would have required repeating *Person.Address* 6 times.
- Shifting of scope via *{#with}* tags allows template builders to assemble extremely re-usable sub-templates. i.e. an Address template can be composed that only needs to know the simple {line1} {City} {State} ...... properties and not be concerned with the context of the parent template.


### Template Comments
The template engine supports *{! comment }* tags.  

##### Data:
```c#
var person = new 
{ 
	Name = new { First = "John", Last = "Doe"}, 
};
```

##### Template:
```
<p>Hello {Name.First},</p>{! we want to keep this greeting informal }
<p>How can we be of assistance?</p>
```

##### Result:
```
<p>Hello John,</p>
<p>How can we be of assistance?</p>
```

##### Notes:
- *{!Comment}* tags can span multiple lines.


### Whitespace Control
By default, all text that resides outside of a *{tag}* is emitted verbatim to output.  Cleanly formatting template blocks can result in un-wanted whitespace copied to output.  When using any non-simple tags ( *{#if}, {#each}, {>}, {!}, {#with}, {?var}* ), the white space trim marker(s) can be applied to the tag for whitespace control. A whitespace trim marker is a single *-* immediately after the open tag delimiter *{-tag}* or immediately before the close tag delimiter *{tag-}* or both *{-tag-}*.

##### Data:
```c#
var person = new 
{ 
	Certifications = new[] { "mcse", "mcitp", "mcts" },
	Name = new { First = "John", Last = "Doe"}, 
};
```

##### Default Template:
```
<p>Hello {Name.First}</p>
<div>
{#if Certifications}
<p>We see you have the following certs:</p>
<ul>
    {#each Certifications}
    <li>{$}</li>
    {/each}
</ul>
{/if}
{#if !Certifications}
We see you don't have any certs.
{/if}
</div>
```

##### Default Output:
```
<p>Hello John</p>
<div>

<ul>
    
    <li>mcse</li>
    
    <li>mcitp</li>
    
    <li>mcts</li>
    
</ul>


</div>
```

##### Whitespace Controlled Template
```
<p>Hello {Name.First}</p>
<div>
{-#if Certifications-}
<p>We see you have the following certs:</p>
<ul>
    {-#each Certifications-}
    <li>{$}</li>
    {-/each-}
</ul>
{-/if-}
{-#if !Certifications-}
We see you don't have any certs.
{-/if-}
</div>
```

##### Whitespace Controlled Output:
```
<p>Hello John</p>
<div>
<ul>
    <li>mcse</li>
    <li>mcitp</li>
    <li>mcts</li>
</ul>
</div>
```

##### Notes:
- Left trim markers *{-#if}* will trim all preceding whitespace INCLUDING the FIRST newline.
- Right trim markers *{#if-}* will trim all trailing whitespace NOT INCLUDING newline(s).
- To force trim on all applicable tags without including the trim markers, set *TemplateEngine.TrimWhitespace = true*.
- If an instance of the template engine has *TrimWhitespace = true*, block template tags can utilize the *'+'* retain whitespace marker to retain whitespace at the tag level.
- The *'+'* retain whitespace trim marker can be used immediately after the open tag delimiter *{+tag}* or immediately before the close tag delimiter *{tag+}* or both.

### Lambda Expressions (Helper Functions)
Formatting, trimming, encoding, uppercasing, lowercasing, sorting, grouping, complex flow control, etc...  A registered function can be called from anywhere within a template including within any sub/partial templates.

##### Lambda Usage
```c#
var person = new 
{ 
	Certifications = new[] { "mcse", "mcitp", "mcts" },
	Name = new { First = "John", Last = "Doe"}, 
};

string template = "Hello {FirstName} {LastName} we see you have these certs: {(', ', Certifications) => join}.";

Func<string, object[], string> join = (delim, values) =>
{
	return string.Join(delim, values);
};

TemplateEngine ngin = new TemplateEngine(template);

ngin.LambdaRepo.Register(nameof(join), join);

string result = ngin.Merge(person);

//result = Hello John Doe we see who have these certs: mcse, mcitp, mcts.
```

##### Notes:
- Lambda expressions can be used within any of the following tags *{simple}*, *{#if}*, *{#each}*, *{#with}* and *{>parital}* tags.
- Lambda arguments can be: a value from the bound object, string literal, numeric literal, or boolean *true/false*.
- Numeric literal argument types are inferred (no need for a type suffix).
- String literal args can be enclosed in single or double quotes.
- If a string literal contains a double quote, enclosing the literal with single quotes to avoid the need to escape.
- I a string literal cotains a single quote, enclose the literal with double quotes to avoid the need to escape.
- If a string literal contains both single and double quotes, the \ backslash char can be used as the escape character.  
  example: "It's easy to escape \\"double\\" quotes."
