@Override
public void handleClientRequest(User user, ISFSObject params) {
    // After SSO authentication success...
    createAndJoinRoom(user);
}

private void createAndJoinRoom(User user) {
    RoomSettings settings = new RoomSettings("Room_" + user.getName() + "_" + System.currentTimeMillis());
    settings.setMaxUsers(20); // Or whatever makes sense.
    settings.setGroupId("default"); // Or your custom group
    settings.setGame(true); // Important if it’s a game room
    settings.setDynamic(true); // Dynamic rooms get auto-removed when empty

    try {
        Room newRoom = getApi().createRoom(getParentExtension().getParentZone(), settings, user);
    } catch (SFSCreateRoomException e) {
        trace("Could not create room: " + e.getMessage());
    }
}

List<Room> rooms = getParentExtension().getParentZone().getRoomList();
for (Room room : rooms) {
    if (room.isDynamic() && room.getUserCount() < room.getMaxUsers()) {
        getApi().joinRoom(user, room);
        return;
    }
}
// If no available room, create a new one
createAndJoinRoom(user);

// C# example, like in Unity
RoomSettings settings = new RoomSettings("Room_" + sfs.MySelf.Name + "_" + DateTime.UtcNow.Ticks);
settings.MaxUsers = 20; // Your choice
settings.IsGame = true; // Important for games
settings.GroupId = "default"; // Or whatever group
settings.IsDynamic = true; // Dynamic = auto-destroyed when empty

// Now create the room
sfs.Send(new CreateRoomRequest(settings, true, sfs.LastJoinedRoom));

<allowClientRoomCreation>true</allowClientRoomCreation>

public class LoginExtension extends SFSExtension {
    @Override
    public void init() {
        addEventHandler(SFSEventType.USER_LOGIN, LoginHandler.class);
    }
}

public class LoginHandler extends BaseServerEventHandler {
    @Override
    public void handleServerEvent(ISFSEvent event) throws SFSException {
        User user = (User) event.getParameter(SFSEventParam.USER);

        RoomSettings settings = new RoomSettings("Room_" + user.getName() + "_" + System.currentTimeMillis());
        settings.setMaxUsers(20); // Or whatever you like
        settings.setGroupId("default");
        settings.setGame(true); // If it's a game room
        settings.setDynamic(true); // Important!

        try {
            getApi().createRoom(getParentExtension().getParentZone(), settings, user);
            // It automatically joins the user after creation!
        } catch (SFSCreateRoomException e) {
            trace("Room creation failed: " + e.getMessage());
            // Handle error (maybe kick user back to lobby)
        }
    }
}

//After SSO


sfs.Send(new CreateRoomRequest(settings, true));

// Assume you're logged in already with SSO!
private void CreateAndJoinRoom()
{
    RoomSettings settings = new RoomSettings("Room_" + sfs.MySelf.Name + "_" + DateTime.UtcNow.Ticks);
    settings.MaxUsers = 20;
    settings.IsGame = true;
    settings.GroupId = "default";
    settings.IsDynamic = true;

    // Send request: create and join immediately
    sfs.Send(new CreateRoomRequest(settings, true));
}

// Then listen for the room join event:
private void OnRoomJoin(BaseEvent evt)
{
    Room joinedRoom = (Room)evt.Params["room"];
    Debug.Log("Successfully joined room: " + joinedRoom.Name);

    // You are still your SSO-logged user
    Debug.Log("My User Name: " + sfs.MySelf.Name);
}

@Override
public void handleServerEvent(ISFSEvent event) throws SFSException {
    Session session = (Session) event.getParameter(SFSEventParam.SESSION);
    String ssoToken = (String) event.getParameter(SFSEventParam.LOGIN_IN_DATA).getUtfString("ssoToken");

    // Verify SSO token here (your company probably has some magic method)
    String verifiedUsername = verifySSOToken(ssoToken);

    // Set the verified username
    LoginEvent loginEvent = (LoginEvent) event.getParameter(SFSEventParam.LOGIN_EVENT);
    loginEvent.setUserName(verifiedUsername);

    // You can also set other Session properties or UserVariables here
}

List<UserVariable> vars = new ArrayList<>();
vars.add(new SFSUserVariable("avatarUrl", "https://example.com/avatar.png"));
vars.add(new SFSUserVariable("accountId", 123456));
getApi().setUserVariables(user, vars);


