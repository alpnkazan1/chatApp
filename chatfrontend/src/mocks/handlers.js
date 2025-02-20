import { http, HttpResponse } from "msw";
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

// Load the image as a Blob (manual conversion step)
async function loadAvatar() {
    const response = await fetch("/avatar_5580993.png"); // Make sure this file is served
    const blob = await response.blob();
    return blob;
}

// Mock handler for user registration
export const handlers = [
    http.post("/auth/register", async ({ request }) => {

        const formdata = await request.formData();
        const email = formdata.get("email");
        const password = formdata.get("password");
        const username = formdata.get("username");
        const avatar = formdata.get("avatar");
    
        // Check missing email, pass or username
        if (!email || !password || !username) {
            return HttpResponse.json(
                { error: "Missing email, password, or username" },
                { status: 400 }
            );
        }
    
        // Check if the email is already in use
        if (email === "name@mail.com") {
            return HttpResponse.json(
                { error: "Email is already in use", code: "email_taken" },
                { status: 400 }
            );
        }

        return HttpResponse.json({
            token: "mockAuthToken",
            message: "Registration successful",
            code: "success"
        });
    }),
    http.post("/auth/login", async ({ request }) => {
        const { email, password } = await request.json();

        if (!email || !password) {
            return HttpResponse.json(
                { error: "Missing email or password", code: "missing_fields" },
                { status: 400 }
            );
        }

        // Mock credentials for testing error cases
        const validUser = {
            email: "user@example.com",
            username: "mock user",
            password: "password",
            isLocked: false
        };

        if (email !== validUser.email || password !== validUser.password) {
            return HttpResponse.json(
                { error: "Invalid email or password", code: "invalid_credentials" },
                { status: 401 }
            );
        }

        if (validUser.isLocked) {
            return HttpResponse.json(
                { error: "Your account has been locked. Contact support.", code: "account_locked" },
                { status: 403 }
            );
        }


        // If the user is valid, create a response with both a token and full user data.
        const response = HttpResponse.json({
            user: {
                email: validUser.email,
                userName: validUser.userName
            },
            token: "mockAuthToken",
            message: "Login successful",
            code: "success"
        });

        // Simulating setting a cookie for authentication token
        response.headers.set("Set-Cookie", "auth_token=mockAuthToken; HttpOnly; Path=/; Max-Age=3600");

        return response;
    }),
    http.post("/auth/logout", async ({ request }) => {
        return new HttpResponse(null, {
            status: 200,
            headers: {
            "Set-Cookie": "token=; Path=/; Max-Age=0", // Clears token cookie
            },
        });
    }),
    http.get("/auth/check", async () => {
        // Simulated user data (excluding avatar)
        const user = {
          uid: "mockUserId123",
          email: "user@example.com",
          userName: "Mock User",
        };
    
        // Simulate an avatar image as binary data (a Blob)
        const avatarBlob = await loadAvatar(); // Fetch the avatar from the served file
        // Convert the avatar blob to an ArrayBuffer and then to a base64 string.
        const arrayBuffer = await avatarBlob.arrayBuffer();
        const uint8Array = new Uint8Array(arrayBuffer);
        const base64String = btoa(String.fromCharCode(...uint8Array));
        

        // Return a JSON response with user data and the base64-encoded avatar.
        return HttpResponse.json({
            user,
            avatar: base64String, // base64 string representing the image
        }, { status: 200 });
    })
];
