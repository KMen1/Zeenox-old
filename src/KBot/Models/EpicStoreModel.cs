using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace KBot.Models;

public partial class EpicStore
{
    [JsonProperty("data")] public Data Data { get; set; }

    private IEnumerable<Game> Games => Data?.Catalog.SearchStore.Games;

    public IEnumerable<Game> CurrentGame => Games?.ToList()
        .FindAll(x => x.Promotions is not null && x.Promotions.PromotionalOffers.Length != 0);
}

public class Data
{
    [JsonProperty("Catalog")] public Catalog Catalog { get; set; }
}

public class Catalog
{
    [JsonProperty("searchStore")] public SearchStore SearchStore { get; set; }
}

public class SearchStore
{
    [JsonProperty("elements")] public Game[] Games { get; set; }

    [JsonProperty("paging")] public Paging Paging { get; set; }
}

public class Game
{
    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("namespace")] public string Namespace { get; set; }

    [JsonProperty("description")] public string Description { get; set; }

    [JsonProperty("effectiveDate")] public DateTimeOffset EffectiveDate { get; set; }

    [JsonProperty("offerType")] public string OfferType { get; set; }

    [JsonProperty("expiryDate")] public object ExpiryDate { get; set; }

    [JsonProperty("status")] public string Status { get; set; }

    [JsonProperty("isCodeRedemptionOnly")] public bool IsCodeRedemptionOnly { get; set; }

    [JsonProperty("keyImages")] public KeyImage[] KeyImages { get; set; }

    [JsonProperty("seller")] public Seller Seller { get; set; }

    [JsonProperty("productSlug")] public string ProductSlug { get; set; }

    [JsonProperty("urlSlug")] public string UrlSlug { get; set; }

    public string EpicUrl => "https://www.epicgames.com/store/en-US/p/" + UrlSlug;

    [JsonProperty("url")] public object Url { get; set; }

    [JsonProperty("items")] public Item[] Items { get; set; }

    [JsonProperty("customAttributes")] public CustomAttribute[] CustomAttributes { get; set; }

    [JsonProperty("categories")] public Category[] Categories { get; set; }

    [JsonProperty("tags")] public Tag[] Tags { get; set; }

    [JsonProperty("catalogNs")] public CatalogNs CatalogNs { get; set; }

    [JsonProperty("offerMappings")] public object[] OfferMappings { get; set; }

    [JsonProperty("price")] public Price Price { get; set; }

    [JsonProperty("promotions")] public Promotions Promotions { get; set; }

    public PromotionalOfferPromotionalOffer[] Discounts => Promotions.PromotionalOffers[0].PromotionalOffers;
}

public class CatalogNs
{
    [JsonProperty("mappings")] public Mapping[] Mappings { get; set; }
}

public class Mapping
{
    [JsonProperty("pageSlug")] public string PageSlug { get; set; }

    [JsonProperty("pageType")] public string PageType { get; set; }
}

public class Category
{
    [JsonProperty("path")] public string Path { get; set; }
}

public class CustomAttribute
{
    [JsonProperty("key")] public string Key { get; set; }

    [JsonProperty("value")] public string Value { get; set; }
}

public class Item
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("namespace")] public string Namespace { get; set; }
}

public class KeyImage
{
    [JsonProperty("type")] public string Type { get; set; }

    [JsonProperty("url")] public Uri Url { get; set; }
}

public class Price
{
    [JsonProperty("totalPrice")] public TotalPrice TotalPrice { get; set; }

    [JsonProperty("lineOffers")] public LineOffer[] LineOffers { get; set; }
}

public class LineOffer
{
    [JsonProperty("appliedRules")] public AppliedRule[] AppliedRules { get; set; }
}

public class AppliedRule
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("endDate")] public DateTimeOffset EndDate { get; set; }

    [JsonProperty("discountSetting")] public AppliedRuleDiscountSetting DiscountSetting { get; set; }
}

public class AppliedRuleDiscountSetting
{
    [JsonProperty("discountType")] public string DiscountType { get; set; }
}

public class TotalPrice
{
    [JsonProperty("discountPrice")] public long DiscountPrice { get; set; }

    [JsonProperty("originalPrice")] public long OriginalPrice { get; set; }

    [JsonProperty("voucherDiscount")] public long VoucherDiscount { get; set; }

    [JsonProperty("discount")] public long Discount { get; set; }

    [JsonProperty("currencyCode")] public string CurrencyCode { get; set; }

    [JsonProperty("currencyInfo")] public CurrencyInfo CurrencyInfo { get; set; }

    [JsonProperty("fmtPrice")] public FmtPrice FmtPrice { get; set; }
}

public class CurrencyInfo
{
    [JsonProperty("decimals")] public long Decimals { get; set; }
}

public class FmtPrice
{
    [JsonProperty("originalPrice")] public string OriginalPrice { get; set; }

    [JsonProperty("discountPrice")] public string DiscountPrice { get; set; }

    [JsonProperty("intermediatePrice")] public string IntermediatePrice { get; set; }
}

public class Promotions
{
    [JsonProperty("promotionalOffers")] public PromotionalOffer[] PromotionalOffers { get; set; }

    [JsonProperty("upcomingPromotionalOffers")]
    public PromotionalOffer[] UpcomingPromotionalOffers { get; set; }
}

public class PromotionalOffer
{
    [JsonProperty("promotionalOffers")] public PromotionalOfferPromotionalOffer[] PromotionalOffers { get; set; }
}

public class PromotionalOfferPromotionalOffer
{
    [JsonProperty("startDate")] public DateTimeOffset StartDate { get; set; }

    [JsonProperty("endDate")] public DateTimeOffset EndDate { get; set; }

    [JsonProperty("discountSetting")] public PromotionalOfferDiscountSetting DiscountSetting { get; set; }
}

public class PromotionalOfferDiscountSetting
{
    [JsonProperty("discountType")] public string DiscountType { get; set; }

    [JsonProperty("discountPercentage")] public long DiscountPercentage { get; set; }
}

public class Seller
{
    [JsonProperty("id")] public string Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; }
}

public class Tag
{
    [JsonProperty("id")] public long Id { get; set; }
}

public class Paging
{
    [JsonProperty("count")] public long Count { get; set; }

    [JsonProperty("total")] public long Total { get; set; }
}

public partial class EpicStore
{
    public static EpicStore FromJson(string json)
    {
        return JsonConvert.DeserializeObject<EpicStore>(json, ConverterEpic.Settings);
    }
}

internal static class ConverterEpic
{
    public static readonly JsonSerializerSettings Settings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        DateParseHandling = DateParseHandling.None,
        Converters =
        {
            new IsoDateTimeConverter {DateTimeStyles = DateTimeStyles.AssumeUniversal}
        }
    };
}