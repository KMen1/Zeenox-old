using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KBot.Modules.EpicFreeGames;

public class StoreModel
{
    [JsonProperty("data")] public Data Data { get; set; }
    //[JsonProperty("extensions")] public Extensions Extensions { get; set; }
}

public class Data
{
    [JsonProperty("catalog")] public Catalog Catalog { get; set; }
}

public class Catalog
{
    [JsonProperty("searchStore")] public SearchStore Search { get; set; }
}

public class SearchStore
{
    [JsonProperty("elements")] public List<Game> Games { get; set; }
}

public class Game
{
    [JsonProperty("title")] public string Title { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    //[JsonProperty("offerType")] public OfferType OfferType { get; set; }
    //[JsonProperty("status")] public Status Status { get; set; }
    [JsonProperty("keyImages")] public List<KeyImage> Images { get; set; }
    [JsonProperty("seller")] public Seller Seller { get; set; }

    [JsonProperty("urlSlug")] public string UrlSlug { get; set; }
    public string Url => "https://www.epicgames.com/store/en-US/p/" + UrlSlug;
    
    [JsonProperty("customAttributes")] public List<CustomAttribute> CustomAttributes { get; set; }
    [JsonProperty("price")] public Price Price { get; set; }
    [JsonProperty("promotions")] public Promotions Promotions { get; set; }
}

public class Promotions
{
    [JsonProperty("promotionalOffers")] public List<PromotionalOffers> PromotionalOffers { get; set; }
}

public class PromotionalOffers
{
    [JsonProperty("promotionalOffers")] public List<PromotionalOffer> Offers { get; set; }
}

public class PromotionalOffer
{
    [JsonProperty("startDate")] public DateTime StartDate { get; set; }
    [JsonProperty("endDate")] public DateTime EndDate { get; set; }
    [JsonProperty("discountSettings")] public DiscountSettings DiscountSettings { get; set; }
}

public class DiscountSettings
{
    [JsonProperty("discountType")] public string DiscountType { get; set; }
    [JsonProperty("discountPercentage")] public int DiscountPercentage { get; set; }
}

public class Price
{
    [JsonProperty("totalprice")] public TotalPrice TotalPrice { get; set; }
}

public class TotalPrice
{
    [JsonProperty("discountPrice")] public int DiscountPrice { get; set; }
    [JsonProperty("originalPrice")] public int OriginalPrice { get; set; }
    [JsonProperty("currencyCode")] public string CurrencyCode { get; set; }
    [JsonProperty("fmtPrice")] public FmtPrice CountryPrice { get; set; }
}

public class FmtPrice
{
    [JsonProperty("originalPrice")] public string OriginalPrice { get; set; }
    [JsonProperty("discountPrice")] public string DiscountPrice { get; set; }
}

public class CustomAttribute
{
    
}

public class Seller
{
    [JsonProperty("name")] public string Name { get; set; }
}

public class KeyImage
{
    //[JsonProperty("type")] public ImageType Type { get; set; }
    [JsonProperty("url")] public string Url { get; set; }
}