import { useState } from "react";
import "./login.css";
import { toast } from "react-toastify";
import { useUserStore } from "../../../lib/userStore";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

const Login = () =>{

    const { fetchUserInfo } = useUserStore();

    const [avatar,setAvatar] = useState({
        file:null,
        url:""
    });

    const [loading, setLoading] = useState(false);

    const handleAvatar = e =>{
        if(e.target.files[0]){
            setAvatar({
                file:e.target.files[0],
                url: URL.createObjectURL(e.target.files[0])
            })
        }
    };

    const handleLogin = async (e) => {
        e.preventDefault();
        setLoading(true);
    
        // Convert form data to a JSON object
        const formData = new FormData(e.target);
        const loginData = Object.fromEntries(formData.entries());
    
        try {
            const response = await fetch(`${API_BASE_URL}/auth/login`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "include", // Ensures cookies are sent and received
                body: JSON.stringify(loginData)
            });
    
            const data = await response.json();
    
            if (response.ok) {
                toast.success("Login successful!");
                fetchUserInfo();
                useUserStore.getState().setAccess(data.accessToken); // Set the access token
                useUserStore.getState().setRefresh(data.refreshToken);  // Set the refresh token
                // No need to store token manually; it's set as an httpOnly cookie by the backend.
                // You can update your auth context or state here if necessary.
            } else {
                // Handle backend error responses
                if (data.code === "invalid_credentials") {
                    toast.error("Invalid email or password!");
                } else if (data.code === "account_locked") {
                    toast.error("Your account has been locked. Contact support.");
                } else {
                    toast.error(data.error || "An error occurred during login!");
                }
            }
        } catch (err) {
            console.log(err);
            toast.error(err.message);
        } finally {
            setLoading(false);
        }
    };

    const handleRegister = async(e) =>{
        
        e.preventDefault();
        setLoading(true);

        const formData = new FormData(e.target);
        if (avatar.file) {
            formData.append("avatar", avatar.file);
        }

        try {

            const response = await fetch(`${API_BASE_URL}/auth/register`, {
                method: "POST",
                body: formData,
            });

            const data = await response.json();

            if (response.ok) {
                toast.success("Account created successfully!");
            } else {
                // Handle backend error responses
                if (data.code === "email_taken") {
                    toast.error("Email is already in use!");
                } else if (data.code === "weak_password") {
                    toast.error("Password is too weak!");
                } else {
                    toast.error(data.error || "Some problem occurred with the creation process!");
                }
            }
        } catch (err){    
            console.log(err);
            toast.error(err.message);
        } finally{
            setLoading(false);
        }
    };

    return <div className="login">
        <div className="item">
            <h2>Welcome back,</h2>
            <form onSubmit={handleLogin}>
                <input type="text" placeholder="Email" name="email" />
                <input type="password" placeholder="Password" name="password" />
                <button disabled={loading}>{loading? "Loading" : "Sign In"}</button>
            </form>
        </div>
        <div className="seperator"></div>
        <div className="item">
            <h2>Create an Account</h2>
            <form onSubmit={handleRegister}>
                <label htmlFor="file">
                    <img src={avatar.url || "./avatar.png"} alt=""/>
                    Upload an image
                </label>
                <input type="file" id="file" style={{display:"none"}} onChange={handleAvatar}/>
                <input type="text" placeholder="Username" name="username" />
                <input type="text" placeholder="Email" name="email" />
                <input type="password" placeholder="Password" name="password" />
                <button disabled={loading}>{loading? "Loading" : "Sign Up"}</button>
            </form>
        </div>
    </div>
};

export default Login;