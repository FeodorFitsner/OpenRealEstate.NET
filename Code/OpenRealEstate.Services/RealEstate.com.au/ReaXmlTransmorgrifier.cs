﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using OpenRealEstate.Core.Models;
using Shouldly;

namespace OpenRealEstate.Services.RealEstate.com.au
{
    public class ReaXmlTransmorgrifier : ITransmorgrifier
    {
        public IList<Listing> Convert(string data)
        {
            data.ShouldNotBeNullOrEmpty();

            var elements = SplitReaXmlIntoElements(data);
            if (!elements.Any())
            {
                return null;
            }

            var listings = new ConcurrentBag<Listing>();

            Parallel.ForEach(elements, new ParallelOptions { MaxDegreeOfParallelism = 1 }, xml => listings.Add(ConvertFromReaXml(xml)));
            //Parallel.ForEach(elements, xml => listings.Add(ConvertFromReaXml(xml)));
            
            return listings.ToList();
        }

        private static IList<string> SplitReaXmlIntoElements(string xml)
        {
            xml.ShouldNotBeNullOrEmpty();

            var doc = XElement.Parse(xml);

            var elements = new List<XElement>();
            elements.AddRange(doc.Descendants("residential").ToList());
            elements.AddRange(doc.Descendants("rental").ToList());

            return elements.Select(x => x.ToString()).ToList();
        }

        private static Listing ConvertFromReaXml(string xml)
        {
            xml.ShouldNotBeNullOrEmpty();

            //var xmlDocument = new XmlDocument { XmlResolver = null };
            //xmlDocument.LoadXml(xml);

            var doc = XElement.Parse(xml);

            // Determine the category, so we know why type of listing we need to create.
            var categoryType = doc.Name.ToCategoryType();

            // We can only handle a subset of all the category types.
            var listing = CreateListing(categoryType);
            if (listing == null)
            {
                // TODO: Add logging message.
                return null;
            }

            // Extract common data.
            ExtractCommonData(listing, doc);
 
            // Extract specific data.
            if (listing is ResidentialListing)
            {
                ExtractResidentialData(listing as ResidentialListing, doc);
            }

            if (listing is RentalListing)
            {
                ExtractRentalData(listing as RentalListing, doc);
            }

            return listing;
        }

        private static Listing CreateListing(CategoryType categoryType)
        {
            Listing listing = null;

            switch (categoryType)
            {
                case CategoryType.Sale:
                    listing = new ResidentialListing();
                    break;
                case CategoryType.Rent:
                    listing = new RentalListing();
                    break;
            }

            return listing;
        }

        #region Common listing methods

        private static void ExtractCommonData(Listing listing, XElement xElement)
        {
            listing.ShouldNotBe(null);
            xElement.ShouldNotBe(null);

            listing.UpdatedOn = ParseReaDateTime(xElement.AttributeValue("modTime"));

            listing.AgencyId = xElement.Value("agentID");
            listing.Id = xElement.Value("uniqueID");
            var status = xElement.AttributeValueOrDefault("status");
            if (!string.IsNullOrWhiteSpace(status))
            {
                listing.StatusType = StatusTypeHelpers.ToStatusType(status);
            }
            var category = xElement.ValueOrDefault("category", "name");
            if (!string.IsNullOrWhiteSpace(category))
            {
                listing.PropertyType = PropertyTypeHelpers.ToPropertyType(category);
            }
            listing.Title = xElement.ValueOrDefault("headline");
            listing.Description = xElement.ValueOrDefault("description");

            listing.Address = ExtractAddress(xElement);
            listing.Agents = ExtractAgent(xElement);
            listing.Features = ExtractFeatures(xElement);
            listing.Inspections = ExtractInspectionTimes(xElement);
            listing.Images = ExtractImages(xElement);
            listing.FloorPlans = ExtractFloorPlans(xElement);
        }

        private static Address ExtractAddress(XElement xElement)
        {
            xElement.ShouldNotBe(null);

            var addressElement = xElement.Element("address");
            if (addressElement == null)
            {
                return null;
            }

            var address = new Address();

            var displayText = addressElement.AttributeValueOrDefault("display");
            address.IsStreetDisplayed = string.IsNullOrWhiteSpace(displayText) || 
                displayText.ParseYesNoToBool();

            var subNumber = addressElement.ValueOrDefault("subNumber");
            address.StreetNumber = string.Format("{0}{1}{2}",
                subNumber,
                string.IsNullOrEmpty(subNumber) ? string.Empty : "/",
                addressElement.ValueOrDefault("streetNumber"));

            address.Street = addressElement.ValueOrDefault("street");

            address.Suburb = addressElement.ValueOrDefault("suburb");

            address.State = addressElement.ValueOrDefault("state");
            
            // REA Xml Rule: Country is ommited == default to Australia.
            // Reference: http://reaxml.realestate.com.au/docs/reaxml1-xml-format.html#country
            var country = addressElement.ValueOrDefault("country");
            address.CountryIsoCode = !string.IsNullOrEmpty(country)
                ? ConvertCountryToIsoCode(country)
                : "AU";

            address.Postcode = addressElement.ValueOrDefault("postcode");

            return address;
        }

        private static IList<ListingAgent> ExtractAgent(XElement xElement)
        {
            xElement.ShouldNotBe(null);

            var agentElements = xElement.Elements("listingAgent").ToArray();
            if (!agentElements.Any())
            {
                return null;
            }

            var agents = new List<ListingAgent>();

            foreach (var agentElement in agentElements)
            {
                var agent = new ListingAgent
                {
                    Name = agentElement.ValueOrDefault("name")
                };

                var orderValue = agentElement.AttributeValueOrDefault("id");
                int order = 0;
                if (!string.IsNullOrWhiteSpace(orderValue) &&
                    int.TryParse(orderValue, out order))
                {
                    agent.Order = order;
                }

                var email = agentElement.ValueOrDefault("email");
                agent.Communications = new  List<Communication>();
                if (!string.IsNullOrWhiteSpace(email))
                {
                    agent.Communications.Add(new Communication
                    {
                        CommunicationType = CommunicationType.Email,
                        Details = email  
                    });
                }
                
                var phoneMobile = agentElement.ValueOrDefault("telephone", "type", "mobile");
                if (!string.IsNullOrWhiteSpace(phoneMobile))
                {
                    agent.Communications.Add(new Communication
                    {
                        CommunicationType = CommunicationType.Landline,
                        Details = phoneMobile
                    });
                }

                var phoneOffice = agentElement.ValueOrDefault("telephone", "type", "BH");
                if (!string.IsNullOrWhiteSpace(phoneOffice))
                {
                    agent.Communications.Add(new Communication
                    {
                        CommunicationType = CommunicationType.Landline,
                        Details = phoneOffice
                    });
                }

                // Some listings have this element but no data provided. :(
                // So we don't add 'emtpy' agents.
                if (!string.IsNullOrWhiteSpace(agent.Name) &&
                    agent.Communications.Any())
                {
                    agents.Add(agent);
                }
            }

            var counter = 0;
            return agents.Any()
                ? agents
                    .OrderBy(x => x.Order)
                    .Select(x => new ListingAgent
                    {
                        Name = x.Name,
                        Order = ++counter,
                        Communications = x.Communications
                    })
                    .ToList()
                : null;
        }

        private static Features ExtractFeatures(XElement xElement)
        {
            xElement.ShouldNotBe(null);

            var featuresElement = xElement.Element("features");
            if (featuresElement == null)
            {
                return null;
            }

            var features = new Features
            {
                Bedrooms = featuresElement.ByteValueOrDefault("bedrooms"),
                Bathrooms = featuresElement.ByteValueOrDefault("bathrooms"),
                CarSpaces = featuresElement.ByteValueOrDefault("garages")
            };

            return features;
        }

        private static IList<Inspection> ExtractInspectionTimes(XElement xElement)
        {
            xElement.ShouldNotBe(null);

            var inspectionTimes = xElement.Element("inspectionTimes");
            if (inspectionTimes == null)
            {
                return null;
            }

            var inspectionElements = inspectionTimes.Elements("inspection").ToList();
            if (!inspectionElements.Any())
            {
                return null;
            }

            var inspections = new List<Inspection>();

            foreach (var inspectionElement in inspectionElements)
            {
                // -Some- xml data only contains empty inspects. (eg. RentalExpress).
                if (inspectionElement == null ||
                    inspectionElement.IsEmpty ||
                    string.IsNullOrWhiteSpace(inspectionElement.Value))
                {
                    continue;
                }

                // Only the following format is accepted as valid: DD-MON-YYYY hh:mm[am|pm] to hh:mm[am|pm]
                // REF: http://reaxml.realestate.com.au/docs/reaxml1-xml-format.html#inspection
                var data = inspectionElement.Value.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                var inspectionStartsOn = DateTime.Parse(string.Format("{0} {1}", data[0], data[1]));
                var inspectionEndsOn = DateTime.Parse(string.Format("{0} {1}", data[0], data[3]));

                var newInspection = new Inspection
                {
                    OpensOn = inspectionStartsOn,
                    ClosesOn = inspectionEndsOn == inspectionStartsOn
                        ? (DateTime?) null
                        : inspectionEndsOn
                };

                // Just to be safe, lets make sure do get dupes.
                if (!inspections.Contains(newInspection))
                {
                    inspections.Add(newInspection);
                }
            }

            return inspections.Any()
                ? inspections.OrderBy(x => x.OpensOn).ToList()
                : null;
        }

        private static IList<Media> ExtractImages(XElement xElement)
        {
            xElement.ShouldNotBe(null);

            var imageElement = xElement.Element("images") ?? xElement.Element("objects");
            if (imageElement == null)
            {
                return null;
            }

            var imagesElements = imageElement.Elements("img").ToArray();
            if (!imagesElements.Any())
            {
                return null;
            }

            var images = (from x in imagesElements
                          let url = x.AttributeValueOrDefault("url")
                          let order = x.AttributeValueOrDefault("id")
                          where !string.IsNullOrWhiteSpace(url)
                          select new Media
                          {
                               Url= url,
                              Order = ConvertImageOrderToNumber(order)
                          }).ToList();

            return images.Any() ? images : null;
        }

        private static IList<Media> ExtractFloorPlans(XElement xElement)
        {
            xElement.ShouldNotBe(null);

            var objectElement = xElement.Element("objects");
            if (objectElement == null)
            {
                return null;
            }

            var floorPlanElements = objectElement.Elements("floorplan").ToArray();
            if (!floorPlanElements.Any())
            {
                return null;
            }

            var floorPlans = (from x in floorPlanElements
                              let url = x.AttributeValueOrDefault("url")
                              let order = x.AttributeValueOrDefault("id")
                              where !string.IsNullOrWhiteSpace(url)
                              select new Media
                              {
                                  Url = url,
                                  Order = System.Convert.ToInt32(order)
                              }).ToList();

            return floorPlans.Any() ? floorPlans : null;
        }

        /// <summary>
        /// REA Specific DateTime parsing.
        /// </summary>
        private static DateTime ParseReaDateTime(string someDateTime)
        {
            someDateTime.ShouldNotBeNullOrEmpty();

            DateTime resultDateTime;
            
            if (!DateTime.TryParse(someDateTime, out resultDateTime))
            {
                DateTime.TryParseExact(someDateTime.Trim(), "yyyy-MM-dd-H:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out resultDateTime);
            }

            if (resultDateTime == DateTime.MinValue)
            {
                DateTime.TryParseExact(someDateTime.Trim(), "yyyyMMdd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out resultDateTime);
            }

            return resultDateTime;
        }

        private static string ConvertCountryToIsoCode(string country)
        {
            country.ShouldNotBeNullOrEmpty();

            switch (country.ToUpperInvariant())
            {
                case "AU":
                case "AUS":
                case "AUSTRALIA" :
                    return "AU";
                case "NZ" :
                case "NEW ZEALAND":
                    return "NZ";
                default : throw new ArgumentOutOfRangeException("country", 
                    string.Format("Country '{0}' is unhandled - not sure of the ISO Code to use.", country));
            }
        }

        private static int ConvertImageOrderToNumber(string imageOrder)
        {
            var character = imageOrder.ToUpperInvariant().First();

            // 65 == 'A'. But we need 'm' to be 1, so we have to do some funky shit.
            // A == 65 - 63 == 2
            // B == 66 - 63 == 3

            return character == 'M' ? 1 : character - 63;
        }

        #endregion

        #region Residential Listing methods

        private static void ExtractResidentialData(ResidentialListing listing, XElement xElement)
        {
            listing.ShouldNotBe(null);
            xElement.ShouldNotBe(null);

            listing.Pricing = ExtractSalePricing(xElement);
        }

        private static SalePricing ExtractSalePricing(XElement xElement)
        {
            xElement.ShouldNotBe(null);

            var salePricing = new SalePricing
            {
                SalePrice = xElement.DecimalValueOrDefault("price")
            };

            var salePriceText = xElement.ValueOrDefault("priceView");
            var displayAttributeValue = xElement.ValueOrDefault("priceView", "display");
            var isDisplay = string.IsNullOrWhiteSpace(displayAttributeValue) || 
                displayAttributeValue.ParseYesNoToBool();
            salePricing.SalePriceText = isDisplay 
                ? salePriceText
                : string.IsNullOrWhiteSpace(salePriceText)
                    ? "Address Witheld"
                    : salePriceText;

            var isUnderOffer = xElement.ValueOrDefault("underOffer", "value");
            salePricing.IsUnderOffer = !string.IsNullOrWhiteSpace(isUnderOffer) &&
                isUnderOffer.ParseYesNoToBool();

            return salePricing;
        }

        #endregion

        #region Rental Listing Methods

        public static void ExtractRentalData(RentalListing rentalListing, XElement xElement)
        {
            rentalListing.ShouldNotBe(null);
            xElement.ShouldNotBe(null);

            rentalListing.Pricing = ExtractRentalPricing(xElement);
        }

        public static RentalPricing ExtractRentalPricing(XElement xElement)
        {
            xElement.ShouldNotBe(null);


            // Quote: There can be multiple rent elements if you wish to specify a price for both monthly and weekly. 
            //        However, at least one of the rent elements must be for a weekly period.
            // Result: FML :(
            var rentElements = xElement.Elements("rent").ToArray();
            if (!rentElements.Any())
            {
                return null;
            }

            // We will only use the WEEKLY one.
            var rentalPricing = new RentalPricing();
            foreach (var re in rentElements)
            {
                var frequency = re.AttributeValueOrDefault("period");
                if (string.IsNullOrWhiteSpace(frequency))
                {
                    continue;
                }

                frequency = frequency.ToUpperInvariant();
                if (frequency != "WEEK" &&
                    frequency != "WEEKLY")
                {
                    continue;
                }

                var rentalPrice = re.Value;
                decimal value;
                if (!string.IsNullOrWhiteSpace(rentalPrice) &&
                    Decimal.TryParse(rentalPrice, out value))
                {
                    rentalPricing.RentalPrice = value;
                }

                break;
            }

            rentalPricing.RentalPriceText = xElement.ValueOrDefault("priceView");
            rentalPricing.Bond = xElement.DecimalValueOrDefault("bond");

            var dateAvailble = xElement.ValueOrDefault("dateAvailable");
            if (!string.IsNullOrWhiteSpace(dateAvailble))
            {
                DateTime date;
                if (DateTime.TryParse(dateAvailble, out date))
                {
                    rentalPricing.AvailableOn = date;
                }
            }

            return rentalPricing;
        }

        #endregion
    }
}