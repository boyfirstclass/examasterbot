    
examasterbot/
    Program.cs

    Models/
        Users/
            UserProfile.cs
            UserRole.cs
        
        Groups/
            GroupInfo.csgit
            GroupTeacher.cs
            GroupStudent.cs
        
        Assignments/
            Assignment.cs
            AssignmentVariant.cs
            Submission.cs
            SubmissionStatus.cs

    Storage/
        Csv/
            CsvStorage.Core.cs
            CsvStorage.Users.cs
            CsvStorage.Groups.cs
            CsvStorage.Assignments.cs
            CsvStorage.Submissions.cs

    Logic/
        BotLogic.Core.cs          
        BotLogic.Users.cs
        BotLogic.Groups.cs
        BotLogic.Assignments.cs   
        BotLogic.Submissions.cs   

    Sessions/
        SessionState.cs
        UserSession.cs

    Tg/
        TgService.Core.cs   
        TgService.Registration.cs
        TgService.Groups.cs
        TgService.Assignments.cs  
        TgService.Submissions.cs  
        TgService.Checking.cs     
        TgService.Help.cs         
        TgService.Formatting.cs   

    Formatting/
        MessageFormatter.cs