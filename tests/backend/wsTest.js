// Test fails because connection times out. 
// But the messages are recorded into database and they are supposedly sent to the entire group
const signalR = require('@microsoft/signalr');
const fs = require('fs');
const assert = require('assert');

describe('SignalR ChatHub - End-to-End Test', () => {
    let connection;
    let testData;
    const serverUrl = 'http://localhost:5148/chatHub';

    before(async () => {
        // Load test data from test.json
        const rawData = fs.readFileSync('test.json');
        testData = JSON.parse(rawData);

        // Ensure the test data is valid before proceeding
        assert(testData.chatId, "ChatId missing in test.json");
        assert(testData.user1Id, "User1Id missing in test.json");
        assert(testData.token, "Token missing in test.json");

        const queryParams = {
            chatId: testData.chatId
        };

        const queryString = Object.keys(queryParams)
            .map(key => key + '=' + queryParams[key])
            .join('&');

        const fullServerUrl = `${serverUrl}?${queryString}`;

        connection = new signalR.HubConnectionBuilder()
            .withUrl(fullServerUrl, {
                accessTokenFactory: () => testData.token
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Log connection state changes for debugging
        connection.onclose(error => {
            console.error("Connection closed: ", error);
        });

    });

    after(async () => {
        if (connection) {
            try {
                await connection.stop();
                console.log('User 1 disconnected');
            } catch (err) {
                console.error('Error while disconnecting:', err);
            }
        }
    });

    it('should connect to the ChatHub', async () => {
      try {
          await connection.start();
          console.log('User 1 connected successfully');
      } catch (err) {
          console.error("Error connecting: ", err);
          assert.fail(err);
      }
  });


    it('should send a message to the ChatHub and receive the newMessage event', (done) => {
        const messageContent = 'Hello again from User 1! (Check the DB manually)';
        const messageData = {
            chatId: testData.chatId,
            receiverId: testData.user2Id,
            messageText: messageContent,
            fileFlag: 0,
            fileId: null,
            fileExtension: null
        };
        try {
          connection.on('newMessage', (message) => {
              console.log('New message received:', message);
              // Check that you received the message
              assert.strictEqual(message.messageText, messageContent); // Changed to message.messageText
              done();
          });

          connection.invoke("SendMessage", messageData).catch(err => {
              console.error("Error while sending message: ", err);
              done(err); // IMPORTANT - Fail the test on error
          });

        } catch(error) {
            console.error("Error on the connection ", error);
            done(error);
        }
    });
});