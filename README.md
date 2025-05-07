# EmailService_Clarity25
You should pull down the whole project and build it. The console and the api can run independently of each other. the api will load swaggerUI when run in debug mode. 
CLI does allow for the recipient email to be passed in as an argument when calling the .exe file.

# Running the EmailService_Clarity25.API
1. Open Postman.
2. Create a new request.
3. Set the request type to POST.
4. Enter the URL of your API endpoint. For example: https://localhost:xxxx/email/send-test-email?recipient=test@example.com (Replace xxxx with the actual port number from your running application).
5. In the URL, make sure to include the recipient query parameter with a valid email address.
6. Click "Send".
7. Postman will show you the API's response. Verify the status code (200 OK for success) and the response body.
