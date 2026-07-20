using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using PentaWork.Tools.PowerPlatform.Common;

namespace PentaWork.Tools.PowerPlatform.Tests.Cli;

/// <summary>Covers the paging loop used by every list/export command: it must carry the PagingCookie
/// from each response into the next request (not just increment PageNumber), and must stop as soon as
/// a response reports no more records.</summary>
[TestClass]
public class PagingCookieTests
{
    [TestMethod]
    public void QueryCore_CarriesPagingCookieForward_AcrossEveryPage()
    {
        var requestedCookies = new List<string?>();
        var requestedPageNumbers = new List<int>();

        var pages = new Queue<EntityCollection>(
        [
            MakePage(["a"], moreRecords: true, cookie: "cookie-1"),
            MakePage(["b"], moreRecords: true, cookie: "cookie-2"),
            MakePage(["c"], moreRecords: false, cookie: null),
        ]);

        // Force the "no PageInfo yet" branch so the first page is deterministically PageNumber 1,
        // rather than depending on QueryExpression's own default PagingInfo.
        var query = new QueryExpression("account") { PageInfo = null! };
        var entities = ServiceClientExtensions.QueryCore(query, getAll: true, q =>
        {
            requestedPageNumbers.Add(q.PageInfo.PageNumber);
            requestedCookies.Add(q.PageInfo.PagingCookie);
            return pages.Dequeue();
        });

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, entities.Select(e => (string)e["idmarker"]).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, requestedPageNumbers);
        // The first request carries no cookie yet; the 2nd/3rd must carry the previous response's cookie forward.
        CollectionAssert.AreEqual(new string?[] { null, "cookie-1", "cookie-2" }, requestedCookies);
    }

    [TestMethod]
    public void QueryCore_StopsAtFirstPage_WhenGetAllIsFalse()
    {
        var callCount = 0;
        var query = new QueryExpression("account");

        var entities = ServiceClientExtensions.QueryCore(query, getAll: false, q =>
        {
            callCount++;
            return MakePage(["a"], moreRecords: true, cookie: "cookie-1");
        });

        Assert.AreEqual(1, callCount, "getAll=false must not follow MoreRecords into a second page.");
        Assert.AreEqual(1, entities.Count);
    }

    [TestMethod]
    public void QueryCore_StopsAsSoonAsMoreRecordsIsFalse()
    {
        var pages = new Queue<EntityCollection>(
        [
            MakePage(["a"], moreRecords: true, cookie: "cookie-1"),
            MakePage(["b"], moreRecords: false, cookie: null),
        ]);
        var callCount = 0;

        var entities = ServiceClientExtensions.QueryCore(new QueryExpression("account"), getAll: true, q =>
        {
            callCount++;
            return pages.Dequeue();
        });

        Assert.AreEqual(2, callCount);
        Assert.AreEqual(2, entities.Count);
    }

    [TestMethod]
    public void QueryCore_DefaultsPageInfo_WhenQueryHasNone()
    {
        QueryExpression? capturedQuery = null;
        var query = new QueryExpression("account") { PageInfo = null! };

        ServiceClientExtensions.QueryCore(query, getAll: false, q =>
        {
            capturedQuery = q;
            return MakePage([], moreRecords: false, cookie: null);
        });

        Assert.IsNotNull(capturedQuery!.PageInfo);
        Assert.AreEqual(1000, capturedQuery.PageInfo.Count);
        Assert.AreEqual(1, capturedQuery.PageInfo.PageNumber);
    }

    private static EntityCollection MakePage(IEnumerable<string> ids, bool moreRecords, string? cookie)
    {
        var collection = new EntityCollection(ids.Select(id => new Entity("account", Guid.NewGuid()) { ["idmarker"] = id }).ToList())
        {
            MoreRecords = moreRecords,
            PagingCookie = cookie
        };
        return collection;
    }
}
