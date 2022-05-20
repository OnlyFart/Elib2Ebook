namespace Elib2Ebook.Types.Litres.Requests; 

public class LitresAuthRequest : LitresRequestBase<LitresAuthData> {
    public LitresAuthRequest(string login, string password) {
        Func = "w_create_sid";
        Param.Login = login;
        Param.Password = password;
    }
}