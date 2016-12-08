CREATE PROCEDURE [dbo].[GetGameUser]
	@UserID nvarchar(50)
AS
	SELECT * from GameUser where UserID = @UserID
RETURN 0
