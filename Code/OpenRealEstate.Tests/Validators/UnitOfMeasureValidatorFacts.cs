﻿using FluentValidation.TestHelper;
using OpenRealEstate.Core.Models;
using OpenRealEstate.Validation;
using Xunit;

namespace OpenRealEstate.Tests.Validators
{
    public class UnitOfMeasureValidatorFacts
    {
        private readonly UnitOfMeasureValidator _unitOfMeasureValidator;

        public UnitOfMeasureValidatorFacts()
        {
            _unitOfMeasureValidator = new UnitOfMeasureValidator();
        }

        [Fact]
        public void GivenAValue_Validate_ShouldNotHaveValidationErrors()
        {
            _unitOfMeasureValidator.ShouldNotHaveValidationErrorFor(unit => unit.Value, 0m);
        }

        [Fact]
        public void GivenANegativeValue_Validate_ShouldHaveValidationErrors()
        {
            _unitOfMeasureValidator.ShouldHaveValidationErrorFor(unit => unit.Value, -1m);
        }

        [Fact]
        public void GivenAValueAndAType_Validate_ShouldNotHaveValidationErrors()
        {
            // Arrange.
            var unitOfMeasure = new UnitOfMeasure
            {
                Type = "a",
                Value = 1
            };

            // Act & Assert.
            _unitOfMeasureValidator.ShouldNotHaveValidationErrorFor(unit => unit.Value, unitOfMeasure);
        }

        [Fact]
        public void GivenAValueButNoType_Validate_ShouldHaveValidationErrors()
        {
            // Arrange.
            var unitOfMeasure = new UnitOfMeasure
            {
                Value = 1
            };

            // Act & Assert.
            _unitOfMeasureValidator.ShouldHaveValidationErrorFor(unit => unit.Type, unitOfMeasure);
        }
    }
}