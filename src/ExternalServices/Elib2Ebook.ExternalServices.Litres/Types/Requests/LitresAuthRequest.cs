namespace Elib2Ebook.ExternalServices.Litres.Types.Requests;

internal class LitresAuthRequest : LitresRequestBase<LitresAuthData>
{
    public LitresAuthRequest(string login, string password)
    {
        Func = "w_create_sid";
        Param.Login = login;
        Param.Password = password;
    }
}
