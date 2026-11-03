# Ubriaco.dll / UbriacoUtil.dll

C#/.NET libraries for model-based SQL data access and common application utilities.

**Ubriaco.dll** maps C# models to database tables and generates `SELECT`, `INSERT`, and `UPDATE` queries. It includes PostgreSQL and SQL Server clients. The examples below use PostgreSQL.

**UbriacoUtil.dll** provides utilities for caching, storage, encryption, OCR, SMTP, version handling, and request flood protection.

Supports **.NET 10** and **.NET 8**.

[Solusek Systems](https://www.solusek.com)

## Build

From the repository root, run:

```bash
dotnet build -c Release
```

Alternatively, use the provided build script:

```bash
./build.sh
```

Use a .NET SDK compatible with the target framework you are building.

## Quick start

### 1. Define a model

This model maps to the PostgreSQL table `public.bookpage`.

```csharp
using Ubriaco.Attributes;
using Ubriaco.Data;
using Ubriaco.Models;

namespace ExampleProject.Models;

public class BookImage
{
    public string FileIdent { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
}

[Table("bookpage")]
public class BookPage : BaseModel
{
    // Maps to BIGINT.
    public long BookID { get; set; }

    // Maps to INT.
    public int PageNumber { get; set; }

    // Strings default to VARCHAR(256).
    // Use [VariableInfo("VARCHAR(500)")] to specify a different length.
    public string? Author { get; set; }

    [VariableInfo("TEXT")]
    public string Text { get; set; } = string.Empty;

    [VariableInfo("TEXT")]
    public string IllustrationDescription { get; set; } = string.Empty;

    // Object properties are serialized as JSON in a TEXT column.
    public BookImage? Image { get; set; }

    // Excluded from database persistence.
    [ModelLocal]
    public string? ExtraStuff { get; set; }

    // Called during SyncStructure.
    public override void CreateIndexes(BaseConnection db)
    {
        base.CreateIndexes(db);

        TryCreateIndex(db, "BookID");
        TryCreateIndex(db, "BookID,PageNumber", true);
    }
}
```

### 2. Configure the connection

Set the default connection string once during application startup. Supply credentials through your application's configuration rather than committing them to source control.

```csharp
using Ubriaco.Data;

BaseConnection.SetConnectionString(connectionString);
```

Example PostgreSQL connection string:

```text
Server=localhost;Port=5432;Database=example;User ID=example;Password=<password>;Pooling=true
```

### 3. Synchronize the schema

Create or update the table structure and indexes for a single model:

```csharp
using ExampleProject.Models;
using Ubriaco.Data;

using (var db = new Database())
{
    new BookPage().SyncStructure(db);
}
```

Or synchronize all discovered models derived from `BaseModel`:

```csharp
using Ubriaco.Data;
using Ubriaco.Toolkit;

// A connection string can also be supplied per connection.
using (var db = new Database(connectionString))
{
    DataManager.SyncAllModels(db);
}
```

Review schema changes before running synchronization against a production database.

### 4. Insert and update

```csharp
using System;
using ExampleProject.Models;
using Ubriaco.Data;

using (var db = await Database.CreateAsync())
{
    var bookPage = new BookPage
    {
        BookID = 1,
        PageNumber = 1,
        Author = "Test",
        Text = "The quick brown fox jumps over the lazy dog.",
        IllustrationDescription = "A fox jumping over a sleeping dog.",
        Image = new BookImage
        {
            FileIdent = Guid.NewGuid().ToString(),
            MimeType = "image/png"
        },
        ExtraStuff = "This value is not stored in the database."
    };

    bookPage.ID = await bookPage.InsertAsync(db);

    bookPage.Author = "Bob";
    await bookPage.UpdateAsync(db, "Author");
}
```

### 5. Select a row

Match a database column against the value on the model instance. This example selects a matching row by `BookID`; use the list example below to retrieve every page for a book.

```csharp
using System;
using ExampleProject.Models;
using Ubriaco.Data;

using (var db = await Database.CreateAsync())
{
    var bookPage = new BookPage { BookID = 1 };

    if (await bookPage.SelectAsync(db, "BookID"))
    {
        Console.WriteLine($"Page ID {bookPage.ID}: {bookPage.Text}");
    }
    else
    {
        Console.WriteLine(
            $"No page found for BookID {bookPage.BookID}.");
    }
}
```

### 6. List matching rows

Retrieve all pages with the specified `BookID`:

```csharp
using System;
using ExampleProject.Models;
using Ubriaco.Data;

using (var db = await Database.CreateAsync())
{
    var bookPage = new BookPage { BookID = 1 };
    var pages = await bookPage.ListAsync(db, "BookID");

    if (pages.Count == 0)
    {
        Console.WriteLine($"No pages found for BookID {bookPage.BookID}.");
    }
    else
    {
        foreach (var page in pages)
        {
            Console.WriteLine($"Page ID {page.ID}: {page.Text}");
        }
    }
}
```

## Libraries and dependencies

### Ubriaco.dll

Model mapping, schema synchronization, and SQL query generation.

| Dependency | Purpose |
| --- | --- |
| Newtonsoft.Json | JSON serialization and parsing |
| Npgsql | PostgreSQL client |
| Microsoft.Data.SqlClient | SQL Server client |

### UbriacoUtil.dll

Helpers and wrappers for caching, storage, rotating encryption, AES-256 encryption, OCR, SMTP, version handling, and locks for flood protection.

| Dependency | Purpose |
| --- | --- |
| StackExchange.Redis | Redis-backed sessions and data caching |
| Tesseract | Optical character recognition |
| AWSSDK.S3 | Amazon S3 storage |
| Google.Cloud.Storage.V1 | Google Cloud Storage |

## Author

**Alessandro Ubriaco**  
[alessandroubriaco.com](https://alessandroubriaco.com)
