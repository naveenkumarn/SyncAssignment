using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using SyncAssignment.Models;
using Newtonsoft.Json.Linq;

namespace SyncAssignment.Controllers
{
    public class SyncServiceController : ApiController
    {
        public dynamic Get(int revision)
        {
            using (DBCustomerEntities context = new DBCustomerEntities())
            {
                int currentRevision = context.Customers.Max(x => x.Revision) ?? 0;
                if (revision == -1)
                {
                    return new
                    {
                        Revision = currentRevision,
                        Customers = context.Customers.Select(x => new
                        {
                            CustomerID = x.CustomerID,
                            Name = x.Name,
                            Email = x.Email,
                            Phone = x.Phone,
                            Revision = x.Revision ?? 0,
                            IsDeleted = x.IsDeleted ?? false

                        }).ToList()
                    };
                }
                else if (revision == currentRevision)
                {
                    return new { Revision = currentRevision };
                }
                else
                {
                    return new
                    {
                        Revision = currentRevision,
                        Customers = context.Customers.Where(x => x.Revision > revision).Select(x => new
                        {
                            CustomerID = x.CustomerID,
                            Name = x.Name,
                            Email = x.Email,
                            Phone = x.Phone,
                            Revision = x.Revision,
                            IsDeleted = x.IsDeleted ?? false
                        }).ToList()
                    };
                }
            }
        }

        private readonly object _updatePointsLock = new object();
        public dynamic Post(JObject data)
        {
            dynamic json = data;
            int revision = json.revision;
            int appID = json.appID;
            IList<Customer> customers = ((JArray)json.customers).ToObject<List<Customer>>();

            lock (_updatePointsLock)
            {
                using (DBCustomerEntities context = new DBCustomerEntities())
                {
                    int currentRevision = context.Customers.Max(x => x.Revision) ?? 0;
                    //check version
                    if (currentRevision == revision)
                    {
                        foreach (Customer cust in customers)
                        {
                            Customer obj = context.Customers.Where(x => x.Email == cust.Email).FirstOrDefault();
                            if (obj == null)
                            {
                                cust.Revision = currentRevision + 1;
                                cust.LastModifiedDate = DateTime.Now;
                                cust.LastModifiedBy = appID;
                                context.Customers.Add(cust);
                            }
                            else
                            {
                                obj.Name = cust.Name;
                                obj.Email = cust.Email;
                                obj.Phone = cust.Phone;
                                obj.IsDeleted = cust.IsDeleted;
                                obj.Revision = currentRevision + 1;
                                obj.LastModifiedDate = DateTime.Now;
                                obj.LastModifiedBy = appID;

                            }

                        }
                        context.SaveChanges();
                        return new
                        {
                            Revision = currentRevision + 1,
                            Customers = context.Customers.Where(x => x.Revision > revision).Select(x => new
                            {
                                CustomerID = x.CustomerID,
                                Name = x.Name,
                                Email = x.Email,
                                Phone = x.Phone,
                                Revision = x.Revision,
                                IsDeleted = x.IsDeleted ?? false
                            }).ToList()
                        };
                    }
                    else
                    {
                        return new { Revision = revision };
                    }
                }
            }

        }

    }
}
