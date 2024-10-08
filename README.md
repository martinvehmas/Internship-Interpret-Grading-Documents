# Grading Document Extraction and Verification System

## Prerequisites


 **Ghostscript**  
   Ghostscript is required for handling PDFs with `MagickImage` (used to convert PDF to images).  
   You can download and install Ghostscript from [here](https://ghostscript.com/releases/gsdnld.html).
   You must also set the bin path of the folder as a PATH enviroment variable.


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
   setx GPT_API_KEY "<your-openai-api-key>"
   ```

   After setting the key, restart your terminal or IDE to ensure the environment variable is loaded.

   ### Mac/Linux:
   Open a terminal and add the following to your `~/.bashrc` or `~/.zshrc` file:

   ```bash
   export GPT_API_KEY="<your-openai-api-key>"
   ```

   Then run the following command to reload your shell:

   ```bash
   source ~/.bashrc
   ```

   (Or replace `.bashrc` with `.zshrc` if you’re using ZSH.)

4. **Verify the API key**  
   Ensure the key is set properly by running:

   ```bash
   echo $GPT_API_KEY
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

- **Ghostscript Errors**:  
   If you encounter issues related to PDF processing, make sure Ghostscript is installed and properly configured and its bin path is in the PATH enviroment variable.

- **API Key Not Working**:  
   If the GPT service fails to run, ensure that your OpenAI API key is correctly set as an environment variable and that it has sufficient funds on your account.
