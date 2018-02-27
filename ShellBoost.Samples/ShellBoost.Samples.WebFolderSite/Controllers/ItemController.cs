using System;
using System.Collections.Generic;
using System.Web.Http;
using ShellBoost.Samples.WebFolderSite.Model;

namespace ShellBoost.Samples.WebFolderSite.Controllers
{
    public class ItemController : ApiController
    {
        // GET api/item
        public IEnumerable<Item> Get()
        {
            return new Item[]
            {
                new Item { Name = "test1" },
                new Item { Name = "test2" },
            };
        }

        // GET api/item/<id>
        public Item Get(Guid id)
        {
            return new Item { Name = "value" };
        }

        // POST api/item
        public void Post([FromBody] Item value)
        {
        }

        // DELETE api/item/<id>
        public void Delete(Guid id)
        {
        }
    }
}