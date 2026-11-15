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

# Defining role of the AI model 

role={
    "1":"Your role is Teacher explain concepts in detaild way",
    "2":"Your role is a traveller explain concepts in detaild way",
    "3":"Your role is a motivational speaker explain concepts in detaild way",
    "4":"Your role is a interviewer explain concepts in detaild way",
    "5":"Your role is a layer explain concepts in detaild way"
}

print("Select a role for the AI model:")
print("1. Teacher")
print("2. Traveller")
print("3. Motivational Speaker")
print("4. Interviewer")
print("5. Lawyer")

choice = input("Enter the number corresponding to your choice: ")



messages = [
    {
        "role": "system", "content": role.get(choice, "Invalid choice")   }
]

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
 