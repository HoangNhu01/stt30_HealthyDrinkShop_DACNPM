﻿using eShopSolution.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace eShopSolution.ViewModels.Catalog.Products
{
    public class IngredientAssignRequest
    {
        public int Id { get; set; }
        public List<SelectItem> Ingredients { get; set; } = new List<SelectItem>();
    }
}
