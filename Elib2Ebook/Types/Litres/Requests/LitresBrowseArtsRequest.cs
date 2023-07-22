namespace Elib2Ebook.Types.Litres.Requests; 

public class LitresBrowseArtsRequest : LitresRequestBase<LitresBrowseArtsData> {
    public LitresBrowseArtsRequest(string[] id) {
        Func = "r_browse_arts";
        Param.Currency = "RUB";
        Param.Anno = "1";
        Param.Id = id;
    }
}