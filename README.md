## Note to team from Fahad (05/04/26):
I'm setting up the repository in this structure:
- One solution (Solution is just a container)
- Solution contains one project called WebApp
- That (WebApp folder) is where all the important files are
- Website follows MVC architecture but it is initialized from ASP.NET Core
  instead of ASP.NET MVC Core.
- Packages installed:
    - EF Core (with MySQL support)
- Database setup:
    - Assumes database with:
        - username: root
        - empty password
        - database name: HMQS
- We are using Picocss for styling (with classes)
- We are using HTMX for updating content using server requests
- We are using Alpine.js for for updating content without server requests
- Layout setup:
    - Sticky navbar
    - Sticky sidebar (left)
    - Sticky playbar (bottom)