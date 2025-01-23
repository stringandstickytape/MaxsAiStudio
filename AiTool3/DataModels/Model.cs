﻿using AiTool3.AiServices;
using System.Diagnostics;

namespace AiTool3.DataModels
{
    public class Model
    {
        public string ModelName { get; set; }

        private ServiceProvider _serviceProvider;
        

        public ServiceProvider Provider
        {
            get { 
                if(_serviceProvider == null)
                {
                    _serviceProvider = new ServiceProvider();
                };
                return _serviceProvider; 
            }
            set
            {
                _serviceProvider = value;
            }
        }

        public decimal input1MTokenPrice { get; set; }
        public decimal output1MTokenPrice { get; set; }
        public Color Color { get; set; }
        public bool Starred { get; set; }

        public string FriendlyName { get; set; }

        private string guid;
        public string Guid {
            get { return guid; }
            set {
                Debug.WriteLine(guid);
                guid = value; 
            }
        }

        public Model()
        {
            Guid = System.Guid.NewGuid().ToString();
        }

        public Model(string guid)
        {
            Guid = guid;
        }

        public bool SupportsPrefill { get; set; }

        public override string ToString()
        {
            return $"{FriendlyName}";
        }

        public string GetCost(TokenUsage tokenUsage)
        {
            var cost = ((tokenUsage.InputTokens * input1MTokenPrice) +
                (tokenUsage.CacheCreationInputTokens * input1MTokenPrice * 1.25m) +
                (tokenUsage.CacheReadInputTokens * input1MTokenPrice * 0.1m) +
                (tokenUsage.OutputTokens * output1MTokenPrice)) / 1000000m;

            return cost.ToString("0.00");
        }
    }
}
