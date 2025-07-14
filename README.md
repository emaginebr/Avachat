# WebooChat

**WebooChat** is an online support platform integrated with **ChatGPT** and **WhatsApp**, designed to provide an intelligent, automated, and personalized conversational experience for businesses and customers.

---

## 🚀 Overview

WebooChat enables businesses to connect with customers via WhatsApp and Web Chat using the power of ChatGPT to automatically respond, log interactions, and escalate to human agents when needed.

---

## ⚙️ Technologies Used

- **Frontend:** [React](https://reactjs.org/)
- **Backend:** [.NET Core 8](https://dotnet.microsoft.com/)
- **Database:** [PostgreSQL](https://www.postgresql.org/)
- **Integrations:**
  - [OpenAI GPT-4](https://platform.openai.com/)
  - [WhatsApp Cloud API](https://developers.facebook.com/docs/whatsapp/cloud-api)

---

## 🧩 Features

- Automated support via ChatGPT
- Direct integration with WhatsApp Business API
- Modern, responsive web interface
- Multi-agent and multi-department support
- Conversation history
- Smart routing (bot → human)
- Admin panel for managing support sessions

---

## 📦 Project Structure

```plaintext
/WebooChat
│
├── backend/            # .NET Core API
│   └── Controllers/
│   └── Services/
│   └── Models/
│
├── frontend/           # React Interface
│   └── src/
│       └── components/
│       └── pages/
│       └── services/
│
├── database/           # PostgreSQL scripts and migrations
│
└── README.md
```

---

## 🛠️ Running Locally

### Prerequisites
- Node.js 18+
- .NET 8 SDK
- PostgreSQL 14+
- Docker (optional, for integrated environment)

### Backend (.NET Core)
```bash
cd backend
dotnet restore
dotnet ef database update
dotnet run
```

### Frontend (React)
```bash
cd frontend
npm install
npm run dev
```

---

## 🔐 Environment Variables

Create a `.env` file with the following variables:

```env
# OpenAI
OPENAI_API_KEY=your_openai_key

# WhatsApp API
WHATSAPP_TOKEN=your_whatsapp_token
WHATSAPP_PHONE_NUMBER_ID=your_number_id

# Database
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=weboochat
POSTGRES_USER=weboo
POSTGRES_PASSWORD=securepassword
```

---

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

---

## 🌐 Live

🔗 [https://weboochat.com](https://weboochat.com)

---

## 🙌 Contribution

Pull requests are welcome! Feel free to suggest improvements or report bugs.
