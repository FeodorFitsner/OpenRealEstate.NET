﻿using OpenRealEstate.Core.Models;
using OpenRealEstate.Services;
using Shouldly;
using Xunit;

namespace OpenRealEstate.Tests
{
    public class ListingHelpersFacts
    {
        public class CopyFacts
        {
            [Fact]
            public void GivenTwoResidentialListings_Copy_CopiesTheData()
            {
                // Arrange.
                var destinationListing = TestHelperUtilities.ResidentialListingFromFile();
                var sourceListing = TestHelperUtilities.ResidentialListingFromFile(false);
                sourceListing.StatusType = StatusType.Sold;
                sourceListing.Pricing.SalePrice = 100;

                // Act.
                ListingHelpers.Copy(destinationListing, sourceListing);

                // Assert.
                destinationListing.StatusType.ShouldBe(sourceListing.StatusType);
                destinationListing.Pricing.SalePrice.ShouldBe(sourceListing.Pricing.SalePrice);
            }

            [Fact]
            public void GivenTwoRentalListings_Copy_CopiesTheData()
            {
                // Arrange.
                var destinationListing = TestHelperUtilities.RentalListing();
                var sourceListing = TestHelperUtilities.RentalListing(false);
                sourceListing.StatusType = StatusType.Leased;
                sourceListing.Pricing.RentalPrice = 100;

                // Act.
                ListingHelpers.Copy(destinationListing, sourceListing);

                // Assert.
                destinationListing.StatusType.ShouldBe(sourceListing.StatusType);
                destinationListing.Pricing.RentalPrice.ShouldBe(sourceListing.Pricing.RentalPrice);
            }

            [Fact]
            public void GivenTwoRuralListings_Copy_CopiesTheData()
            {
                // Arrange.
                var destinationListing = TestHelperUtilities.RuralListing();
                var sourceListing = TestHelperUtilities.RuralListing(false);
                sourceListing.StatusType = StatusType.Leased;
                sourceListing.Pricing.SalePrice = 100;

                // Act.
                ListingHelpers.Copy(destinationListing, sourceListing);

                // Assert.
                destinationListing.StatusType.ShouldBe(sourceListing.StatusType);
                destinationListing.Pricing.SalePrice.ShouldBe(sourceListing.Pricing.SalePrice);
            }
        }
    }
}