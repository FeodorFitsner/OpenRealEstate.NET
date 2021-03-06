﻿using Shouldly;
using Xunit;

namespace OpenRealEstate.Tests.Models
{
    public class RentalListingFacts
    {
        public class CopyFacts
        {
            [Fact]
            public void GivenAnExistingListingAndANewListingWithEverythingModified_Copy_CopiesOverTheData()
            {
                // Arrange.
                var sourceListing =TestHelperUtilities.RentalListingFromFile(false);
                var destinationListing =TestHelperUtilities.RentalListing();

                // Act.
                destinationListing.Copy(sourceListing);

                // Assert.
                destinationListing.PropertyType.ShouldBe(sourceListing.PropertyType);
                destinationListing.IsPropertyTypeModified.ShouldBe(true);
                destinationListing.AvailableOn.ShouldBe(sourceListing.AvailableOn);
                destinationListing.IsAvailableOnModified.ShouldBe(true);

                destinationListing.Pricing.RentalPrice.ShouldBe(sourceListing.Pricing.RentalPrice);
                destinationListing.Pricing.IsRentalPriceModified.ShouldBe(true);
                destinationListing.Pricing.RentalPriceText.ShouldBe(sourceListing.Pricing.RentalPriceText);
                destinationListing.Pricing.IsRentalPriceTextModified.ShouldBe(true);
                destinationListing.Pricing.PaymentFrequencyType.ShouldBe(sourceListing.Pricing.PaymentFrequencyType);
                destinationListing.Pricing.IsPaymentFrequencyTypeModified.ShouldBe(true);
                destinationListing.Pricing.Bond.ShouldBe(sourceListing.Pricing.Bond);
                destinationListing.Pricing.IsBondModified.ShouldBe(true);
                destinationListing.IsPricingModified.ShouldBe(true);

                destinationListing.BuildingDetails.Area.Type.ShouldBe(sourceListing.BuildingDetails.Area.Type);
                destinationListing.BuildingDetails.Area.IsTypeModified.ShouldBe(true);
                destinationListing.BuildingDetails.Area.Value.ShouldBe(sourceListing.BuildingDetails.Area.Value);
                destinationListing.BuildingDetails.Area.IsValueModified.ShouldBe(true);
                destinationListing.BuildingDetails.IsAreaModified.ShouldBe(true);
                destinationListing.BuildingDetails.EnergyRating.ShouldBe(sourceListing.BuildingDetails.EnergyRating);
                destinationListing.BuildingDetails.IsEnergyRatingModified.ShouldBe(true);
                destinationListing.IsBuildingDetailsModified.ShouldBe(true);
            }

            [Fact]
            public void GivenAnExistingListingAndANewListingWithANullValues_Copy_CopiesOverTheData()
            {
                // Arrange.
                var sourceListing =TestHelperUtilities.RentalListingFromFile();
                sourceListing.BuildingDetails = null;
                sourceListing.Pricing = null;

                var destinationListing =TestHelperUtilities.RentalListing();

                // Act.
                destinationListing.Copy(sourceListing);

                // Assert.
                destinationListing.BuildingDetails.ShouldBe(null);
                destinationListing.IsBuildingDetailsModified.ShouldBe(true);
                destinationListing.Pricing.ShouldBe(null);
                destinationListing.IsPricingModified.ShouldBe(true);
            }
        }

        public class IsModifiedFacts
        {
            [Fact]
            public void GivenAnExistingListingAndIdUpdated_IsModified_ReturnsTrue()
            {
                // Arrange.
                var listing = TestHelperUtilities.RentalListingFromFile();
                listing.IsModified.ShouldBe(false);

                // Act.
                listing.Id = "pewpew";

                // Assert.
                listing.IsModified.ShouldBe(true);
            }
        }

        public class IsBuildingDetailsModifiedFacts
        {
            [Fact]
            public void GivenAnExistingListingAndBuildingDetailsEnergyRatingUpdated_IsBuildingDetailsModified_ReturnsTrue()
            {
                // Arrange.
                var listing = TestHelperUtilities.RentalListingFromFile();
                listing.IsBuildingDetailsModified.ShouldBe(false);

                // Act.
                listing.BuildingDetails.EnergyRating = 3;

                // Assert.
                listing.IsBuildingDetailsModified.ShouldBe(true);
            }
        }

        public class IsPricingModifiedFacts
        {
            [Fact]
            public void GivenAnExistingListingAndPricingRentalPriceUpdated_IsPricingModified_ReturnsTrue()
            {
                // Arrange.
                var listing = TestHelperUtilities.RentalListingFromFile();
                listing.IsPricingModified.ShouldBe(false);

                // Act.
                listing.Pricing.RentalPrice = 30;

                // Assert.
                listing.IsPricingModified.ShouldBe(true);
            }
        }
    }
}