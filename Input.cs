public class Input
{
    public static void main(string[] args)
    {
        var x = 4 + 3;
        Test(x);
    }

    public static void Test(int x)
    {
        var z = 1;

        if (x > 5)
        {
            Console.WriteLine("Greater");
        }
        else
        {
            Console.WriteLine("Smaller");
        }

        var message = "vlakas";
        Hix(message);
    }

    private static void Hix(string message)
    {
        Console.WriteLine($"EIMAI O HIX, {message}");
    }
}
