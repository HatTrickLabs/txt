### Usage:

```c#
var fullName = new { FirstName = "Jerrod", LastName = "Eiman"};
string template = "Hello {FirstName} {LastName}, this is just a test.";
TemplateEngine ngin = new TemplateEngine(template);
string result = ngin.Merge(fullName);
//result: Hello Jerrod Eiman, this is just a test.
```

### Simple Tags

Data:

```c#
var fullName = new { FirstName = "Jerrod", LastName = "Eiman"};
```
Template:

```mustache
Hello {FirstName} {LastName}, this is just a test.
```

Result:

```
Hello Jerrod Eiman, this is just a test.
```


### Simple Tags with Compound Expressions

Data:
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

Template:
Hello {Name.First}, we see you currently live in {Address.City}, {Address.State}.

Result: 
Hello Jerrod, we see you currently live in Dallas, TX.


### Conditional Template Blocks:

Data:
var person = new 
{ 
	IsEmployed = true, 
	Employer = "Hat Trick Labs"
	Name = new { First = "Jerrod", Last = "Eiman"}, 
};

Template:
{FirstName} {LastName} {#if IsEmployed}is currently employed at {Employer}{/if}{#if !IsEmployed}is currently unemployed{/if}

Result:
Jerrod Eiman is currently employed at Hat Trick Labs.

Notes: 
- The second if block is negated with the ! logic negation operator.
- Condition blocks are not rendered for falsey values.  Falsey values include: false, null, 0, empty string, empty collection.
- Missing values are not considered Falsey.  Attempted bind to a property that does not exist on the bound object will throw an exception.


### Iteration Template Blocks

Data:
var person = new 
{ 
	Employer = "Hat Trick Labs",
	Certifications = new[] { "mcse", "mcitp", "mcts" },
	Name = new { First = "Jerrod", Last = "Eiman"}, 
};

Template:
Hello {Name.First} {Name.Last},

{#if Certifications}
We see you currently hold the following certs:
{#each Certifications}
  - {$}
{/each}
{/if}
{#if !Certifications}
We see you currently do not have ANY certs...
{/if}

Result:
Hello Jerrod Eiman,

We see you currently hold  the following certs:
  - mcse
  - mcitp
  - mcts

Notes:
- Each blocks bound to Falsy values (null or empty) result in no block content rendered.
- The $ reserved varible always references the root value of local scope (this) and can be used in any tag within a template.
- the ..\ operator can be used to walk the scope chain.


### Partial Templates Blocks

Data:
string parital = "<li><bold>{$.Id}</bold> - {$.LastName}, {$.FirstName}</li>;
var attendees = new
{ 
	People = new []
	{
		{ Id = 1, FirstName = "Jerrod", LastName = "Eiman"},
		{ Id = 2, FirstName = "John", LastName = "Doe"},
		{ Id = 3, FirstName = "Jane", LastName = "Smith"}
	},
	RsvpFormat = partial
}

Template:
<ul>
	{#each People}	
	{>RsvpFormat}
	{/each}
</ul>

Result:
<ul>
	<li><bold>1</bold> - Eiman, Jerrod</li>
	<li><bold>2</bold> - Doe, John</li>
	<li><bold>3</bold> - Smith, Jane</li>
</ul>


### Lambda Expressions (Helper Functions)

Data:
var person = new 
{ 
	Certifications = new[] { "mcse", "mcitp", "mcts" },
	Name = new { First = "Jerrod", Last = "Eiman"}, 
};

Code:
Func<string[], string, string> Join = (values, delimiter) =>
{
	return string.Join(values, delimiter);
};

Template:
<p>{Name.First} {Name.Last} has the following certs:</p>
<p>{(Certifications, ', ') => Join}</p>

Result:
<p>Jerrod Eiman has the following certs:</p>
<p>mcse, mcitp, mcts</p>

Notes:
- Lambda expressions can be used for simple tags, #if tags, #each tags, and >parital tags.

