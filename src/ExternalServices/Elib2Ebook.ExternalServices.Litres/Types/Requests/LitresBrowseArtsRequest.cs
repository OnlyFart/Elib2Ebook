namespace Elib2Ebook.ExternalServices.Litres.Types.Requests;

internal class LitresBrowseArtsRequest : LitresRequestBase<LitresBrowseArtsData>
{
    public LitresBrowseArtsRequest(string[] id)
    {
        Func = "r_browse_arts";
        Param.Currency = "RUB";
        Param.Anno = "1";
        Param.Id = id;
    }
}
