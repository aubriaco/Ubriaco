Ubriaco.dll
=======

Welcome to the C#, PostgreSQL, SQL Server data layer and utilities library.

Ubriaco.dll maps class objects directly into PostgreSQL tables. Automatically creates SELECT/INSERT/UPDATE queries for every data model.

Home page: [www.solusek.com](https://www.solusek.com)

Compiling
-------------

Run: `./build.sh` or `dotnet build -c Release`

Example
-------------

C# .NET 8 (also supports .NET 6)

A model for table `public.bookpage`.

```C#
using Ubriaco.Models;
using Ubriaco.Attributes;

namespace ExampleProject.Models;

[Table("bookpage")]
public class BookPage : BaseModel
{
    // A BIGINT property in table bookpage
    public long BookID { get; set; }

    // A INT property in table bookpage
    public int PageNumber { get; set; }

    // By default all strings are VARCHAR(256) unless a VariableInfo is specified Ex. [VariableInfo("VARCHAR(500)")]
    public string? Author { get; set; }

    // Creates/updates a TEXT column in table bookpage
    [VariableInfo("TEXT")]
    public string Text { get; set; } = string.Empty;

    // Creates/updates a TEXT column in table bookpage
    [VariableInfo("TEXT")]
    public string IllustrationDescription { get; set; } = string.Empty;

    // Any object based property will be stored in a TEXT column as a JSON serialization
    public BookImage? Image { get; set; }

    // ModelLocal property does not get stored in database
    [ModelLocal]
    public string? ExtraStuff { get; set; }


    // This gets executed on SyncStructure
    public override void CreateIndexes(BaseConnection db)
    {
        base.CreateIndexes();

        // Normal Index
        TryCreateIndex(db, "BookID");

        // Unique Index
        TryCreateIndex(db, "BookID,PageNumber", true);
    }
}
```

Initiate single model - create table and indexes.

```C#
using Ubriaco.Data;

// Set global connection string
BaseConnection.SetConnectionString("Server=localhost;Port=5432;Database=test;User ID=test;Password=test;Pooling=true");

// Non-async connection
using(var db = new Database())
{
    new BookPage().SyncStructure(db);
}
```

Initiate all models.

```C#
using Ubriaco.Data;
using Ubriaco.Toolkit;

// Connection string can also be specified per connection
using(var db = new Database("Server=localhost;Port=5432;Database=test;User ID=test;Password=test;Pooling=true"))
{
    // Will find all models that extend BaseModel and call their SyncStructure method
    DataManager.SyncAllModels(db);
}
```

Insert and update data.

```C#
using Ubriaco.Models;

// Async connection
using(var db = await Database.CreateAsync())
{
    var bookPage = new BookPage()
    {
        BookID = 1,
        PageNumber = 1,
        Author = "Test",
        Text = "The quick brown fox jumps over the lazy dog.",
        IllustrationDescription = "A fox jumping over a sleeping dog",
        Image = new BookImage()
        {
            FileIdent = Guid.NewGuid().ToString(),
            MimeType = "image/png"
        },
        ExtraStuff = "This is not stored in the database."
    };
    bookPage.ID = await bookPage.InsertAsync(db);

    bookPage.Author = "Bob";

    await bookPage.UpdateAsync(db, "Author");
}

```

Select data.

```C#
using(var db = await Database.CreateAsync())
{
    var bookPage = new BookPage() { BookID = 1 };

    // Select single row by matching BookID with the value on object: 1
    if(await bookPage.SelectAsync(db, "BookID"))
    {
        Console.WriteLine($"Found book with increment ID {bookPage.ID} text: {bookPage.Text}");
    }
    else
    {
        Console.WriteLine($"BookPage with BookID {bookPage.ID} not found.")
    }
}
```

List data.

```C#
using(var db = await Database.CreateAsync())
{
    var bookPage = new BookPage() { BookID = 1 };

    // Selects all rows available by matching BookID with the value on object: 1
    var list = await bookPage.ListAsync(db, "BookID");

    if(list.Count > 0)
    {
        foreach(var item in list)
        {
            Console.WriteLine($"Increment ID {item.ID} text: {item.Text}");
        }
    }
    else
    {
        Console.WriteLine($"No BookPage rows with BookID {bookPage.ID} found.")
    }
}
```

Author
-------------

Alessandro Ubriaco
[www.alessandroubriaco.com](https://alessandroubriaco.com)
