using System.Linq;
using FastTests;
using Raven.NewClient.Abstractions.Indexing;
using Raven.NewClient.Client.Indexes;
using Xunit;

namespace SlowTests.Issues
{
    public class RavenDB_422 : RavenNewTestBase
    {
        [Fact]
        public void UsingStoreAllFields()
        {
            using (var store = GetDocumentStore())
            {
                new UserIndex().Execute(store);
                using (var session = store.OpenSession())
                {
                    session.Store(new User
                    {
                        Name = "aye",
                        Email = "de"
                    });
                    session.SaveChanges();
                }

                using (var session = store.OpenSession())
                {
                    var r = session.Advanced.DocumentQuery<dynamic>("UserIndex")
                        .WaitForNonStaleResults()
                        .SelectFields<dynamic>("UN", "UE")
                        .Single();

                    Assert.Equal("aye", r.UN.ToString());
                    Assert.Equal("de", r.UE.ToString());
                }
            }
        }

        private class User
        {
            public string Name { get; set; }
            public string Email { get; set; }
        }

        private class UserIndex : AbstractIndexCreationTask<User>
        {
            public UserIndex()
            {
                Map = users =>
                      from user in users
                      select new { UN = user.Name, UE = user.Email };

                StoreAllFields(FieldStorage.Yes);
            }
        }
    }
}