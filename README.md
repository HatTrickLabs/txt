### Usage:

```c#
var fullName = new { FirstName = "Jerrod", LastName = "Eiman"};
string template = "Hello {FirstName} {LastName}, this is just a test.";
TemplateEngine ngin = new TemplateEngine(template);
string result = ngin.Merge(fullName);
//result = Hello Jerrod Eiman, this is just a test.
```

### Simple Tags
In its simplest form, the template engine can be used to inject data into text templates via {tag} replacement.

##### Data:
```c#
var fullName = new { FirstName = "Jerrod", LastName = "Eiman"};
```
##### Template:
```mustache
Hello {FirstName} {LastName}, this is just a test.
```

##### Result:
```
Hello Jerrod Eiman, this is just a test.
```


### Simple Tags with Compound Expressions
Simple {tag}s can contain compound bind expressions to reference data from nested object structures.

##### Data:
```c#
var person = new 
{ 
	Name = new { First = "Jerrod", Last = "Eiman"}, 
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
```mustache
Hello {Name.First}, we see you currently live in {Address.City}, {Address.State}.
```

##### Result: 
```
Hello Jerrod, we see you currently live in Dallas, TX.
```


### Conditional Template Blocks:
The {#if} tag allows for conditionally rendering template blocks based on evaluation of truthy/falsy conditions.

##### Data:
```c#
var person = new 
{ 
	IsEmployed = true, 
	Employer = "Hat Trick Labs"
	Name = new { First = "Jerrod", Last = "Eiman"}, 
};
```

##### Template:
```mustache
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
Hello Jerrod Eiman,
We see you are currently employed at Hat Trick Labs.
```

##### Notes: 
- The second if block is negated with the ! logic negation operator.
- Condition blocks are not rendered for falsey values.  Falsey values include:
	* false boolean
	* null
	* numeric zero
	* empty string
	* empty collection
- Missing values are not considered Falsey.  Attempted bind to a property that does not exist on the bound object will throw an exception.


### Iteration Template Blocks
The {#each} tag allows for conditional rendering based on collection types.  The {#each} tag will iterate over each item in the provided
collection and render the contained text block.  The contained text block operates within the scope context of the iterated item.

##### Data:
```c#
var person = new 
{ 
	Employer = "Hat Trick Labs",
	Certifications = new[] { "mcse", "mcitp", "mcts" },
	Name = new { First = "Jerrod", Last = "Eiman"}, 
};
```
##### Template:

```mustache
Hello {Name.First} {Name.Last},

We see you currently hold the following certs:
{#each Certifications}
  - {$}
{/each}
{#if !Certifications}
  - We see you currently do not have ANY certs...
{/if}
```

##### Result:
```
Hello Jerrod Eiman,

We see you currently hold  the following certs:
  - mcse
  - mcitp
  - mcts
```

##### Notes:
- An each block bound to a Falsy value (null or empty) will result in no block content rendered.
- Each tags work on any object that implements the System.Collections.IEnumerable interface.
- The $ reserved varible always references the root value of local scope (this).  $ can be used in any tag within a template.
- the ..\ operator can be used to walk the scope chain.



### Partial Templates Blocks
The partial template {>tag} is used to inject sub template content.  

##### Data:
```c#
var attendees = new
{ 
	People = new []
	{
		{ Id = 1, FirstName = "Jerrod", LastName = "Eiman"},
		{ Id = 2, FirstName = "John", LastName = "Doe"},
		{ Id = 3, FirstName = "Jane", LastName = "Smith"}
	},
	RsvpFormat = "<li><bold>{$.Id}</bold> - {$.LastName}, {$.FirstName}</li>"
}
```

##### Template:
```mustache
<ul>
	{#each People}	
	{>RsvpFormat}
	{/each}
</ul>
```

##### Result:
```
<ul>
	<li><bold>1</bold> - Eiman, Jerrod</li>
	<li><bold>2</bold> - Doe, John</li>
	<li><bold>3</bold> - Smith, Jane</li>
</ul>
```


### Template Comments
The template engine supports {! comment } tags.  

##### Data:
```c#
var person = new 
{ 
	Name = new { First = "Jerrod", Last = "Eiman"}, 
};
```

##### Template:
```mustachio
<p>Hello {Name.First},</p>{! We want to keep an informal greeting here. }
<p>How can we be of assistance?</p>
```

##### Result:
```
<p>Hello Jerrod,</p>
<p>How can we be of assistance?</p>
```

### Lambda Expressions (Helper Functions)

##### Data:
```c#
var person = new 
{ 
	Certifications = new[] { "mcse", "mcitp", "mcts" },
	Name = new { First = "Jerrod", Last = "Eiman"}, 
};
```

##### Code:
```c#
Func<string[], string, string> Join = (values, delimiter) =>
{
	return string.Join(values, delimiter);
};
```

##### Template:
```mustache
<p>{Name.First} {Name.Last} has the following certs:</p>
<p>{(Certifications, ', ') => Join}</p>
```

##### Result:
```
<p>Jerrod Eiman has the following certs:</p>
<p>mcse, mcitp, mcts</p>
```

##### Notes:
- Lambda expressions can be used for simple tags, #if tags, #each tags, and >parital tags.

