fetch('test.json')
      .then(response => response.json()) // Parse the JSON
      .then(data => {
        // Get token from the JSON
        const token = data.token;

        // Encode the token using Base64 to avoid special character issues
        const encodedToken = btoa(`Bearer ${token}`);

        // Create a WebSocket connection and send token as subprotocol
        const socket = new WebSocket("ws://localhost:5148/chatHub", [encodedToken]);

        socket.onopen = () => {
          console.log("WebSocket connected!");
          socket.send("Hello, Server!"); // Send message once connected
        };

        socket.onmessage = (event) => {
          console.log("Message from server:", event.data);
        };

        socket.onclose = () => {
          console.log("WebSocket closed");
        };
        
        socket.onerror = (error) => {
          console.error("WebSocket error:", error);
        };
      })
      .catch(error => {
        console.error("Error loading JSON:", error);
      });