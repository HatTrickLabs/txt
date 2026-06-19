# HatTrick.Text.Templating

[![NuGet](https://img.shields.io/nuget/v/HatTrick.Text.Templating.svg)](https://www.nuget.org/packages/HatTrick.Text.Templating/)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

A small, allocation-conscious text templating engine for .NET.

**[Full documentation](https://hattricklabs.com/docs/text-templates/)** | **[NuGet package](https://www.nuget.org/packages/HatTrick.Text.Templating/)** | **[hattricklabs.com](https://hattricklabs.com)**

---

## Installation
The package targets *net9.0*.
```bash
dotnet add package HatTrick.Text.Templating
```
```c#
using HatTrick.Text.Templating;
```



## Basic Usage:
```c#
var fullName = new { FirstName = "John", LastName = "Doe"};

string template = "Hello {FirstName} {LastName}, this is just a test.";

TemplateEngine ngin = new TemplateEngine(template);

string result = ngin.Merge(fullName);

//result = Hello John Doe, this is just a test.
```



## Simple Tags
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
- If a template contains any non-tag brackets, they can be escaped by doubling them up. {{abc}} will render {abc} into the output.
- The $ character is reserved by the template engine and represents `this`.
- $ can be used to reference local scope within any tag anywhere within a template.
- ```Hello {$.FirstName}``` and ```Hello {FirstName}``` are functionally equivalent templates.
- Usage of $ is optional and only needed when iterating scalar values via an iteration loop.
- Unquoted Whitespace within tags is ALWAYS insignificant.



## Tags with Compound Bind Expressions
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



## Conditional Blocks:
The *{#if}* tag allows for conditionally rendering template blocks based on evaluation of *truthy/falsy* conditions.

##### Data:
```c#
var person = new 
{ 
	IsEmployed = true, 
	Employer = "Hat Trick Labs",
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



## Iteration Blocks
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

We see you currently hold the following certs:
  - mcse
  - mcitp
  - mcts
```

##### Notes:
- An each block bound to a *falsy* value (null or empty) will result in no block content rendered.
- *{#each}* tags work on any object that implements the *System.Collections.IEnumerable* interface.
- The $ reserved varible always references the root value of local scope (*this*).  
- The value of $ changes every time scope changes and can be used within any template tag.
- The ..\ operator can be used to walk backwards through scope chain (see *Scope Chain Walk* below).
- Within the *{#each}* block from the example in this section, a tag can reference the outer each block scope
  by walking back one level *{..\Employer}*.  Declaring variables is a better way of accessing outer scope and 
  is described in detail within the next section.



## Scope Chain Walk
Inside nested *{#each}* or *{#with}* blocks, local scope shifts to the iterated/with target.  The *..\\* operator walks one scope level backward per occurrence and can be chained (e.g. *{..\\..\\Foo}*).

##### Data:
```c#
var report = new 
{ 
    Title = "Quarterly Sales",
    Departments = new[] 
    {
        new 
        { 
            Name = "Sales",
            Reps = new[] 
            { 
                new { Name = "Alice" }, 
                new { Name = "Bob" } 
            }
        }
    }
};
```

##### Template:
```
{Title}
{#each Departments}
  {Name}:
  {#each Reps}
  - {Name} (dept: {..\Name}, report: {..\..\Title})
  {/each}
{/each}
```

##### Result:
```
Quarterly Sales
  Sales:
  - Alice (dept: Sales, report: Quarterly Sales)
  - Bob (dept: Sales, report: Quarterly Sales)
```

##### Notes:
- The *..\\* operator may be followed by any bind expression — properties, the *$* root, or further dotted access.
- For more than one or two walks deep, declaring a variable in the outer scope is usually cleaner than chaining *..\\* operators.



## Variable Declaration
The variable declaration tag is used to declare and store a local template variable.  *{?var:xyz=$.Name}* declares a local variable named xyz and sets it's value to $.Name *(this.Name)*.  The assignment portion of the variable declaration tag is optional.  The variable declaration tag *{?var:abc}* simply declares a variable and leaves the value equal to null.  Once a variable has been declared it can be reassigned via the variable reassignment tag *{?:xyz = "hello"}*.  The *var* keyword is left out when reassigning.

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
[dbo].[Person].[BirthDate] date
```
##### Notes:
- Declaring, assigning and referencing a variable requires the variable name be proceeded by a colon:
	* Declaration: *{?var:myVar = $ }*
	* Usage: *{:myVar}*
	* Reassignment *{?:myVar = "hello"}*
- The colon ensures no collisions between declared variable names and properties, fields or keys of the bound object.
- Variables can be set via string literals, numeric literals, a value from the bound object, lambda expressions or boolean *true/false*:
	* String Literal: *{?var:someText = "Hello"}*
	* Numeric Literal: *{?var:someNum = 3.0}*
	* Bound Expression: *{?var:someVal = $.SomeProperty}*
	* Lambda: *{?var:someVal = () => GetSomeValue}*
	* Boolean: *{?var:isValid = true}*
- String literal values can be wrapped in double quotes or single quotes.
- Numeric literal values are stored unparsed and coerced to the target type when consumed by a typed lambda parameter — no type suffix required.
- Declaring a variable without a value leaves it *null* — *{?var:nothing}*.  This is useful if a template designer needs to explicitly pass *null* to a lambda function: *{(:nothing) => MyFunc}*.



## Variable Reassignment
Once declared, a variable can be reassigned with the *{?:name = value}* tag (the *var* keyword is omitted).  Reassignment of an undeclared variable throws.  Variables are looked up by walking the scope chain outward, so an inner block can reassign a variable declared in an outer scope.

##### Data:
```c#
var data = new { Items = new[] { "apple", "banana", "cherry" } };
```

##### Template (with *TrimWhitespace = true*):
```
{?var:count = 0}
{#each Items}
{?:count = (1, :count) => add}
{:count}. {$}
{/each}
Total: {:count}
```

```c#
ngin.LambdaRepo.Register("add", (Func<int, int, int>)((a, b) => a + b));
ngin.TrimWhitespace = true;
```

##### Result:
```
1. apple
2. banana
3. cherry
Total: 3
```

##### Notes:
- *{?var:name}* declares — uses the *var* keyword.
- *{?:name = value}* reassigns — no *var* keyword.
- *{:name}* reads.



## Variable Scope Lifetime
Variables declared inside a block tag (*{#if}*, *{#each}*, *{#with}*, partials) live only for the duration of that block.  When the block ends — or each iteration of a *{#each}* completes — variables declared within that scope are released.  Variables declared in an outer scope remain accessible (and reassignable) from inner blocks.

##### Example:
```
{?var:outer = "visible everywhere"}
{#each Items}
  {?var:inner = "visible only in this iteration"}
  {:outer} / {:inner}
{/each}
{:outer}
{:inner}  <-- throws: inner was released when the each block exited
```

##### Notes:
- Each *{#each}* iteration starts a fresh inner scope, so re-declaring the same variable name across iterations is safe.
- To accumulate a value across iterations, declare the variable in the outer scope and reassign from inside the loop (as shown in *Variable Reassignment* above).



## Partial Template Blocks
The partial template *{>tag}* is used to inject sub template content.  

##### Data:
```c#
var attendees = new
{ 
	People = new []
	{
		new { Id = 1, FirstName = "John", LastName = "Doe"},
		new { Id = 2, FirstName = "John", LastName = "Doe"},
		new { Id = 3, FirstName = "Jane", LastName = "Smith"}
	},
	RsvpFormat = "<li><bold>{$.Id}</bold> - {$.LastName}, {$.FirstName}</li>"
}
```

##### Template:
```
<ul>
	{#each People}	
	{>$.RsvpFormat}
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



## With Blocks
The *{#with}* template tag allows for a shift of local scope to a different position in the bound object.

##### Data:
```c#
var account = new 
{ 
	Person = new 
	{
		Name = new { First = "John", Last = "Doe" },
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
- Shifting of scope via *{#with}* tags allows template builders to assemble extremely re-usable sub-templates. i.e. an Address template can be composed that only needs to know the simple *{Line1} {Line2} {City} {State}* and *{Zip}* properties and not be concerned with the context of the parent template.



## Template Comments
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
- *{!Comment}* tags can contain single bracket characters *{* and *}* and double bracket character sets *{{* and *}}*.
- If the *{!Comment}* tag does contain any bracket characters, they must have matching open and close sets.  The parser assumes a close bracket is the end of the comment tag if there is no corresponding open bracket.
- If the comment must contain a close bracket with no corresponding open bracket, the close bracket must be escaped with a backslash as follows:
```{! test comment with escaped close bracket \} }```



## Whitespace Control
By default, all text that resides outside of a *{tag}* is emitted verbatim to output.  Cleanly formatting template blocks can result in un-wanted whitespace copied to output.  When using any non-simple tags ( *{#if}, {#each}, {>}, {!}, {#with}, {?var}, {?}, {@}* ), the white space trim marker(s) can be applied to the tag for whitespace control. A whitespace trim marker is a single *-* immediately after the open tag delimiter *{-tag}* or immediately before the close tag delimiter *{tag-}* or both *{-tag-}*.

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
- Left trim markers *{-#if}* will trim all preceding whitespace NOT INCLUDING newline(s).
- Right trim markers *{#if-}* will trim all trailing whitespace INCLUDING the first encountered newline.
- To force trim on all applicable tags without including the trim markers, set *TemplateEngine.TrimWhitespace = true*.
- If an instance of the template engine has *TrimWhitespace = true*, block template tags can utilize the *'+'* retain whitespace marker to retain whitespace at the tag level.
- The *'+'* retain whitespace trim marker can be used immediately after the open tag delimiter *{+tag}* or immediately before the close tag delimiter *{tag+}* or both.

##### Retain Whitespace Example:
With *TrimWhitespace = true*, every eligible tag trims by default.  Use *+* on a specific tag to opt that tag out and keep its surrounding whitespace intact.

```c#
ngin.TrimWhitespace = true;
```

##### Template:
```
<ul>
{+#each Items+}
  <li>{$}</li>
{+/each+}
</ul>
```

##### Result:
```
<ul>

  <li>A</li>

  <li>B</li>

</ul>
```

Without the *+* markers, the same template under *TrimWhitespace = true* would collapse to *<ul>  <li>A</li>  <li>B</li></ul>*.



## Lambda Expressions
Formatting, trimming, encoding, uppercasing, lowercasing, sorting, grouping, complex flow control, etc...  A registered function can be called from anywhere within a template including within any sub/partial templates.  The funcion call syntax is argument list enclosed in parenthesis followed by the lambda operator then the function name:  
```{ (arg1, arg2, arg3) => funcName }```

##### Lambda Usage
```c#
var person = new 
{ 
	Certifications = new[] { "mcse", "mcitp", "mcts" },
	Name = new { First = "John", Last = "Doe"}, 
};

string template = "Hello {Name.First} {Name.Last} we see you have these certs: {(', ', Certifications) => join}.";

Func<string, object[], string> join = (delim, values) =>
{
	return string.Join(delim, values);
};

TemplateEngine ngin = new TemplateEngine(template);

ngin.LambdaRepo.Register(nameof(join), join);

string result = ngin.Merge(person);

//result = Hello John Doe we see you have these certs: mcse, mcitp, mcts.
```

##### Notes:
- Lambda expressions can be used within any of the following tags *{simple}*, *{#if}*, *{#each}*, *{#with}*, *{?var:},* *{?}*, *{>parital}* and *{@}* tags.
- Lambda arguments can be: a value from the bound object, string literal, numeric literal, boolean *true/false* literal or a declared variable,.
- To explicitly pass a *null* argument to a lambda function, declare an unassigned variable and pass that variable as the argument — see [Variable Declaration](#variable-declaration).
- Numeric literal argument types are inferred (no need for a type suffix).
- String literal args can be enclosed in single or double quotes.
- If a string literal contains a double quote, enclose the literal with single quotes to avoid the need to escape.
- If a string literal cotains a single quote, enclose the literal with double quotes to avoid the need to escape.
- If a string literal contains both single and double quotes, the \ backslash char can be used as the escape character.  
  example: "It's easy to escape \\"double\\" quotes."



## Debugging
The debug tag allows developers to troubleshoot template rendering by providing a mechanism for emitting content through the .NET *System.Diagnostics.Trace* framework.  The following debug tag
```{@ 'Got to line 50' }``` would emit the *Got to line 50* string literal to all registered trace listeners.  The default trace listener, which is pre-registered within Visual Studio and VS code, will write the debug information to the IDE Output Window.  Custom trace listeners can be registered via the *System.Diagnostics.Trace.Listeners.Add(listener);* method to redirect the debug
output to a different medium (see [Microsoft](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.trace.listeners) docs for additional info).

##### Data:
```c#
var data = new { 
	FirstName = "John", LastName = "Doe",
	FavoriteColors = new[] { "Blue", "Red", "Green" }  
};
```
##### Template:
```
Hello {FirstName} {LastName},

Here is a list of your favorite colors:
{@ 'starting #each block for colors' }
{#each FavoriteColors}
{@ $ }
- {$}
{/each}
{@ 'completed #each block for colors' }
```

##### Template Result:
```
Hello John Doe,

Here is a list of your favorite colors:
- Blue
- Red
- Green
```

##### Output Window Result:
```
starting #each block for colors
Blue
Red
Green
completed #each block for colors
```

##### Notes:
- Debug tags have zero impact on template merge output.
- Debug tags can emit string literals, numeric literals, boolean true/false literals, data from the bound object, or the result of a lambda function call.
- String literals can be single or double quoted.
- Double quoted literals can contain un-escaped single quotes.
- Single quoted literals can contain un-escaped double quotes.
- If your string literal requires both a single and double quote, you can escape quotation marks with the backslack character.



## Exception Handling
Any exception thrown from within the *TemplateEngine.Merge()* function will bubble out to the consumer as a *HatTrick.Text.Templating.MergeException*.  The *MergeException* class is a wrapper exception and will contain the actual thrown instance within the *InnerException* property.  The *MergeException* class provides valuable troubleshooting information such as the line number, column position and char index from the exact location within a template where an exception is thrown.  This contextual awareness is available via the *MergeException.Context* property. 

##### MergeException members:
- *InnerException* — the underlying exception thrown during merge.
- *Context* — a *MergeExceptionContextStack* (a *Stack<MergeExceptionContext>*) describing the location at each engine frame.

##### MergeExceptionContext members:
- *Line* — 1-based line number in the template where the error occurred.
- *Column* — 1-based column number on that line.
- *CharIndex* — 0-based character index into the template string.
- *LastTag* — the most recently parsed tag string, when available.

##### Example:
```c#
try
{
    string output = ngin.Merge(data);
}
catch (MergeException mex)
{
    foreach (var frame in mex.Context)
        Console.WriteLine(frame); // Ln: 4   Col: 12   Char Index: 87   LastTag: {Foo.Bar}

    Console.WriteLine(mex.InnerException);
}
```

##### Notes:
- The *MergeException.Context* property provides a stack of *MergeExceptionContext* instances that can be used to pinpoint the exact location within a template where an exception is thrown.
- Why is the *Context* property a stack?   The template engine instantiates additional instances of itself when rendering content for partial tags or blocked content from block tags (*{#if}, {#each} and {#with}*).  Each nested engine pushes its own context frame.



## Stack Depth Limit
The template engine spawns a sub-engine for each *{#if}*, *{#each}*, *{#with}* and partial template block.  The maximum nesting depth is *TemplateEngine.MaxStack* (currently *64*).  Templates that exceed this depth — most commonly via recursive partials — surface as a *MergeException* whose *InnerException* is a *TemplateStackDepthException* (a subclass of *InvalidOperationException*) with the message "Stack depth overflow...".



## License
Apache-2.0.  See the [LICENSE](LICENSE) file in the repository root.
