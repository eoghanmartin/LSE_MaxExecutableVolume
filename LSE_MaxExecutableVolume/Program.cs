using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LSE_MaxExecutableVolume
{
    static class Program
    {
        private static readonly List<Order> OrderList = new List<Order>();

        static void Main(string[] args)
        {
            var fileLocation = "";
            try
            {
                if (args == null)
                {
                    Console.WriteLine("No TestAuctionBook file location specified.");
                }
                else
                {
                    fileLocation = args[0];
                }

                ReadFile(fileLocation);

                SetUpOrderVolumes();

                var executableOrder = GetExecutableOrder();
                if (executableOrder != null)
                {
                    Console.WriteLine("Price: " + executableOrder.Price);
                    Console.WriteLine("Volume: " + executableOrder.Volume);
                }
                else
                {
                    Console.WriteLine("Could not find an executable order.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }

        private static void SetUpOrderVolumes()
        {
            var i = 0;
            foreach (var order in OrderList)
            {
                var buyVolume = 0;
                var sellVolume = 0;
                var orderCount = 0;
                foreach (var orderVolume in OrderList)
                {
                    if (orderCount >= i)
                    {
                        sellVolume = sellVolume + orderVolume.Sell;
                    }
                    if (orderCount <= i)
                    {
                        buyVolume = buyVolume + orderVolume.Buy;
                    }
                    orderCount++;
                }
                order.Volume = sellVolume > buyVolume ? buyVolume : sellVolume;
                i++;
            }
        }

        private static List<Order> GetMaxExecutableVolume()
        {
            var maxExecutableVolumeOrders = new List<Order>();
            var maxVolume = 0;
            foreach (var order in OrderList)
            {
                if (order.Volume >= maxVolume)
                {
                    maxVolume = order.Volume;
                }
            }
            foreach (var order in OrderList)
            {
                if (order.Volume == maxVolume)
                {
                    maxExecutableVolumeOrders.Add(order);
                }
            }
            return maxExecutableVolumeOrders;
        }

        private static List<Order> GetMinSurplus(IReadOnlyCollection<Order> maxExecutableVolumeOrders)
        {
            var minSurplusOrders = new List<Order>();
            var minSurplusVolume =
                Math.Abs(maxExecutableVolumeOrders.First().Sell - maxExecutableVolumeOrders.First().Buy);
            if (maxExecutableVolumeOrders.Count > 1)
            {
                foreach (var order in maxExecutableVolumeOrders)
                {
                    var surplus = Math.Abs(order.Sell - order.Buy);
                    if (surplus <= minSurplusVolume)
                    {
                        minSurplusVolume = surplus;
                    }
                }
                foreach (var order in maxExecutableVolumeOrders)
                {
                    var surplus = Math.Abs(order.Sell - order.Buy);
                    if (surplus == minSurplusVolume)
                    {
                        minSurplusOrders.Add(order);
                    }
                }
            }
            return minSurplusOrders;
        }

        private static Order GetExecutableOrder()
        {
            var maxExecutableVolumeOrders = GetMaxExecutableVolume();
            List<Order> minSurplusOrders;

            if (maxExecutableVolumeOrders.Count > 1)
            {
                minSurplusOrders = GetMinSurplus(maxExecutableVolumeOrders);
            }
            else
            {
                return maxExecutableVolumeOrders.First();
            }
            var noSurplus = new List<Order>();
            var pressureBalanceBuy = new List<Order>();
            var pressureBalanceSell = new List<Order>();
            if (minSurplusOrders.Count > 1)
            {
                foreach (var order in minSurplusOrders)
                {
                    var surplus = order.Sell - order.Buy;
                    if (surplus < 0)
                    {
                        pressureBalanceBuy.Add(order);
                    }
                    else if (surplus > 0)
                    {
                        pressureBalanceSell.Add(order);
                    }
                    else
                    {
                        noSurplus.Add(order);
                    }
                }
                if (noSurplus.Count == 1)
                {
                    return noSurplus.First();
                }
                if (noSurplus.Count > 1)
                {
                    //take lowest price.
                    var orderToExecute = new Order(0, 0, 0);
                    var lowestPrice = noSurplus.First().Price;
                    foreach (var order in noSurplus)
                    {
                        if (order.Price >= lowestPrice) continue;
                        lowestPrice = order.Price;
                        orderToExecute = order;
                    }
                    return orderToExecute;
                }
                if ((pressureBalanceBuy.Any()) && (pressureBalanceSell.Any()))
                {
                    //take lowest price.
                    var lowestPrice = pressureBalanceSell.First().Price;
                    var orderToExecute = pressureBalanceSell.First();
                    foreach (var order in pressureBalanceSell)
                    {
                        if (order.Price >= lowestPrice) continue;
                        lowestPrice = order.Price;
                        orderToExecute = order;
                    }
                    foreach (var order in pressureBalanceBuy)
                    {
                        if (order.Price >= lowestPrice) continue;
                        lowestPrice = order.Price;
                        orderToExecute = order;
                    }
                    return orderToExecute;
                }
                if (pressureBalanceBuy.Any())
                {
                    var orderToExecute = new Order(0, 0, 0);
                    var highestPrice = 0;
                    foreach (var order in pressureBalanceBuy)
                    {
                        if (order.Price <= highestPrice) continue;
                        highestPrice = order.Price;
                        orderToExecute = order;
                    }
                    return orderToExecute;
                }
                if (pressureBalanceSell.Any())
                {
                    var orderToExecute = new Order(0, 0, 0);
                    var lowestPrice = pressureBalanceSell.First().Price;
                    orderToExecute = pressureBalanceSell.First();
                    foreach (var order in pressureBalanceSell)
                    {
                        if (order.Price >= lowestPrice) continue;
                        lowestPrice = order.Price;
                        orderToExecute = order;
                    }
                    return orderToExecute;
                }
            }
            else if (minSurplusOrders.Count == 1)
            {
                return minSurplusOrders.First();
            }
            return null;
        }

        private static void ReadFile(string filename)
        {
            var fileAuctionOrders = File.ReadAllText(filename);
            var auctionOrders = Regex.Split(fileAuctionOrders, "\r\n");
            foreach (var order in auctionOrders)
            {
                if (order == "") continue;
                var orderSummary = order.Split('\t');
                var buy = Int32.Parse(orderSummary[0]);
                var price = Int32.Parse(orderSummary[1]);
                var sell = Int32.Parse(orderSummary[2]);
                var newOrder = new Order(buy, price, sell);
                OrderList.Add(newOrder);
            }
        }
    }

    public class Order
    {
        public int Price { get; private set; }

        public int Buy { get; private set; }

        public int Sell { get; private set; }

        public int Volume { get; set; }

        public Order(int buy, int price, int sell)
        {
            Buy = buy;
            Price = price;
            Sell = sell;
        }
    }
}
