from pyexpat.errors import messages

from openai import OpenAI
from dotenv import load_dotenv
import os

# Load configuration from the .env file
load_dotenv()

# Create a client that communicates with Ollama
client = OpenAI(
    base_url=os.getenv("BASE_URL"),
    api_key=os.getenv("API_KEY")
)
messages = []

while True:
    name = input("Whats on your mind? ")
    #Save UsersInput
    messages.append({"role": "user", "content": name})
    # Send a question to the AI model
    response = client.chat.completions.create(
        model=os.getenv("MODEL"),
        messages=messages
    )

    Ai_Replay = response.choices[0].message.content

    # Display the response
   
    if name.lower() in ["exit", "quit"]:
        break
    print(Ai_Replay)

    messages.append({"role": "assistant", "content": Ai_Replay})
    print(messages)