import ChatList from "./chatList/Chatlist";
import "./list.css";
import UserInfo from "./userinfo/UserInfo";

const List = () => {
    return(
        <div className="list">
        <UserInfo/>
        <ChatList/>
        </div>
    )
}

export default List;