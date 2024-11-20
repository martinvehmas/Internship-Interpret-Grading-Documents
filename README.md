# Grading Document Extraction and Verification System

You can use the demo at https://interpret-grading-documents.azurewebsites.net/

## Installation

Follow these steps to set up and run the project:

1. **Clone the repository**  
   Open a terminal/command prompt and run:

   ```bash
   git clone <repository-url>
   ```

2. **Navigate to the project directory**  
   Change into the project folder:

   ```bash
   cd <project-directory>
   ```

3. **Restore the dependencies**  
   Use the following command to restore all required packages:

   ```bash
   dotnet restore
   ```

4. **Build the project**  
   Run the build command to compile the project:

   ```bash
   dotnet build
   ```

## Setup OpenAI API

To use GPT models in this project, you need an OpenAI API key. Follow these steps to obtain and configure the API key:

1. **Create an OpenAI account**  
   Go to [OpenAI's official website](https://openai.com) and create an account if you don't have one.

2. **Generate an API key**  
   After logging in, go to your [OpenAI API keys page](https://platform.openai.com/account/api-keys). Click on "Create New Secret Key" to generate a new API key.

3. **Set the API key in the environment**  
   You need to set the API key in your environment variables. Depending on your operating system, follow one of the following steps:

   ### Windows:
   Open a command prompt or PowerShell and run:

   ```bash
   setx OPENAI_API_KEY "<your-openai-api-key>"
   ```

   After setting the key, restart your terminal or IDE to ensure the environment variable is loaded.

   ### Mac/Linux:
   Open a terminal and add the following to your `~/.bashrc` or `~/.zshrc` file:

   ```bash
   export OPENAI_API_KEY="<your-openai-api-key>"
   ```

   Then run the following command to reload your shell:

   ```bash
   source ~/.bashrc
   ```

   (Or replace `.bashrc` with `.zshrc` if you’re using ZSH.)

4. **Verify the API key**  
   Ensure the key is set properly by running:

   ```bash
   echo $OPENAI_API_KEY
   ```

   It should display your OpenAI API key.

## Running the Project

Once you have completed all the steps above, follow these commands to run the project:

1. **Run the project**  
   Use this command to start the application:

   ```bash
   dotnet run
   ```

2. **Access the Application**  
   Once the project is running, navigate to `http://localhost:7102` in your web browser.

3. **Upload and Analyze a Document**  
   You can upload a PDF or image file (JPG, PNG) to the application. The system will extract the information, and you will be able to review and correct any inaccuracies in the extracted data.

## Troubleshooting

- **API Key Not Working**:  
   If the GPT service fails to run, ensure that your OpenAI API key is correctly set as an environment variable and that it has sufficient funds on your account.

## Gallery

![qLlj35d](https://github.com/user-attachments/assets/156aed74-2f2a-4db5-9681-596f024a5157)
![NP00aMA](https://github.com/user-attachments/assets/f7b0e6c5-8103-42c9-939b-d36c7f243a6e)
![bgXf3DM](https://github.com/user-attachments/assets/c7dc5111-93e3-4488-b28a-d90f5766268e)
![Kh2EZkD](https://github.com/user-attachments/assets/ff2863da-b254-44ef-a5c2-9687194c1e2e)
![kjbIUb5](https://github.com/user-attachments/assets/f7e70fb1-226a-42b4-a56f-4bf67796310a)
![Z2Qufrm](https://github.com/user-attachments/assets/d64d3a89-7446-4a87-9986-f35e56a4a69c)


