﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace APIViewWeb
{
    public class CosmosReviewRepository
    {
        private readonly Container _reviewsContainer;

        public CosmosReviewRepository(IConfiguration configuration)
        {
            var client = new CosmosClient(configuration["Cosmos:ConnectionString"]);
            _reviewsContainer = client.GetContainer("APIView", "Reviews");
        }

        public async Task<IEnumerable<ReviewModel>> GetReviewsAsync(bool closed)
        {
            var allReviews = new List<ReviewModel>();
            var queryDefinition = new QueryDefinition("SELECT * FROM Reviews r WHERE" +
                                                      "(IS_DEFINED(r.IsClosed) ? r.IsClosed : false) = @isClosed")
                .WithParameter("@isClosed", closed);

            var itemQueryIterator = _reviewsContainer.GetItemQueryIterator<ReviewModel>(queryDefinition);
            while (itemQueryIterator.HasMoreResults)
            {
                var result = await itemQueryIterator.ReadNextAsync();
                allReviews.AddRange(result.Resource);
            }

            return allReviews.OrderByDescending(r => r.LastUpdated);
        }

        public async Task UpsertReviewAsync(ReviewModel reviewModel)
        {
            await _reviewsContainer.UpsertItemAsync(reviewModel, new PartitionKey(reviewModel.ReviewId));
        }

        public async Task DeleteReviewAsync(ReviewModel reviewModel)
        {
            await _reviewsContainer.DeleteItemAsync<ReviewModel>(reviewModel.ReviewId, new PartitionKey(reviewModel.ReviewId));
        }

        public async Task<ReviewModel> GetReviewAsync(string reviewId)
        {
            return await _reviewsContainer.ReadItemAsync<ReviewModel>(reviewId, new PartitionKey(reviewId));
        }
    }
}