using System.Text.RegularExpressions;
using Tesseract;

namespace Ubriaco.Util;

public class OCRUtil
{
    private string TessDataPath;
    private List<ItemPrice> _ItemPriceList = new List<ItemPrice>();

    public List<ItemPrice> ItemPriceList
    {
        get
        {
            return _ItemPriceList;
        }
    }

    public class ItemPrice
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool WithSign { get; set; } = false;
        public bool WithDot { get; set; } = false;
        public int X { get; set; }
        public int Y { get; set; }
    }

    public OCRUtil(string tessdataPath)
    {
        TessDataPath = tessdataPath;
    }

    public void Process(string lang, byte[] data)
    {
        using (var engine = new TesseractEngine(TessDataPath, lang, EngineMode.Default))
        {
            using (var img = Pix.LoadFromMemory(data))
            {
                using (var page = engine.Process(img))
                {
                    var lvl = PageIteratorLevel.TextLine;
                    using (var it = page.GetIterator())
                    {
                        it.Begin();
                        do
                        {

                            if (it.TryGetBoundingBox(lvl, out var rect))
                            {
                                var ls = it.GetText(lvl).Replace("\r", "").Trim();

                                try
                                {
                                    Console.WriteLine("OCR: RECT:\t" + rect.X1 + " " + rect.Y1 + "\t\tTEXT: " + ls);

                                    var item = ExtractItem(ls, rect.X1, rect.Y1);
                                    if(item != null)
                                    {
                                        Console.WriteLine("OCR: Item: Name: " + item.Name + " Price: " + item.Price + " WithSign: " + item.WithSign + " WithDot: " + item.WithDot);
                                        _ItemPriceList.Add(item);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("Error: " + ex.Message);
                                    Console.WriteLine("Line Data: " + ls);
                                }
                            }


                        } while (it.Next(lvl));
                    }
                }
            }
        }
    }
    

    public ItemPrice? ExtractItem(string str, int x, int y)
    {
        string name, priceStr;
        var regex = new Regex(@"(?<!\w)(\$|€|£)?\s?\d{1,3}(?:,\d{3})*(?:\.\d{2})?(?!\w)");
        var matches = regex.Matches(str);

        if(matches.Count == 1)
        {
            priceStr = matches[0].Value.Replace("$", string.Empty).Replace(" ", string.Empty);
            name = str.Replace(matches[0].Value, string.Empty);
            decimal price;

            if(!string.IsNullOrWhiteSpace(name) && decimal.TryParse(priceStr, out price))
                return new ItemPrice() { Name = name, Price = price, WithSign = true };
        }

        regex = new Regex(@"(?<!\w)(?:\d+[.,]\d+|[.,]\d+|\d+)(?!\w)");
        matches = regex.Matches(str);

        if(matches.Count == 1)
        {
            priceStr = matches[0].Value.Replace(",", string.Empty);
            name = str.Replace(matches[0].Value, string.Empty);
            decimal price;

            if(!string.IsNullOrWhiteSpace(name) && decimal.TryParse(priceStr, out price))
                return new ItemPrice()
                {
                    Name = name,
                    Price = price,
                    WithDot = matches[0].Value.Contains('.'),
                    X = x,
                    Y = y
                };
        }

        return null;
    }
}